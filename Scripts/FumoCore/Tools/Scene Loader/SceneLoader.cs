using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace rinCore
{
    public class SceneLoader : MonoBehaviour
    {
        [SerializeField] private ScenePairSO editorStartingScene, buildStartingScene;
        [SerializeField] private bool LoadStartingSceneInBuild;
        [SerializeField] private GameObject loadingScreen;
        [SerializeField] private Image fadingImage;
        [SerializeField] private TMP_Text loadingScreenText;

        internal static SceneLoader Instance { get; private set; }

        private static ScenePairSO _currentScenePair;

        private static Dictionary<string, SceneInstance> _loadedAddressableAdditives = new();
        private static SceneInstance _currentMainSceneInstance;
        private static bool _hasActiveMainSceneInstance = false;

        public static bool IsLoading { get; private set; }

        public static event Action WhenStartLoadingAdditives;
        public static event Action WhenFinishedLoadingAdditives;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                if (loadingScreen != null) loadingScreen.SetActive(false);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        private IEnumerator Start()
        {
            var initHandle = Addressables.InitializeAsync();
            yield return initHandle;
            if (editorStartingScene != null)
            {
                yield return null;
#if UNITY_EDITOR
                LoadScenePair(editorStartingScene, new SceneLoadSettings { Payload = null, Delay = 0f });
#else
                if (LoadStartingSceneInBuild)
                    LoadScenePair(buildStartingScene, new SceneLoadSettings { Payload = null, Delay = 5f });
#endif
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ResetStatics()
        {
            Instance = null;
            _currentScenePair = null;
            _loadedAddressableAdditives = new Dictionary<string, SceneInstance>();
            _hasActiveMainSceneInstance = false;
            IsLoading = false;
        }

        #region Public Wrapper
        public struct SceneLoadSettings
        {
            public Action Payload, PostUnloadPayload;
            public float Delay;
            public float FadeIn, FadeOut;
        }
        public static void MainMenu()
        {
            if (Instance.editorStartingScene is ScenePairSO p)
                LoadScenePair(p);
        }
        public static void LoadScenePair(ScenePairSO pair, SceneLoadSettings? settings = null)
        {
            SceneLoadSettings finalSettings = settings ?? new SceneLoadSettings()
            {
                Delay = 0f,
                FadeIn = 0.1f,
                FadeOut = 0.1f
            };
            if (Instance != null && !IsLoading)
            {
                if (pair == _currentScenePair)
                {
                    WhenStartLoadingAdditives?.Invoke();
                    WhenFinishedLoadingAdditives?.Invoke();
                    finalSettings.Payload?.Invoke();
                    if (Instance.loadingScreen != null) Instance.loadingScreen.SetActive(false);
                    return;
                }
                Instance.StartCoroutine(Instance.CO_LoadScenePair(pair, finalSettings));
            }
        }
        #endregion

        #region Core Coroutine
        private IEnumerator CO_LoadScenePair(ScenePairSO pair, SceneLoadSettings settings)
        {
            if (pair == null || IsLoading) yield break;
            IsLoading = true;

            Application.backgroundLoadingPriority = ThreadPriority.High;

            EventSystem cachedEventSystem = null;
            if (EventSystem.current is EventSystem e)
            {
                cachedEventSystem = e;
                cachedEventSystem.enabled = false;
            }

            if (settings.Delay > 0f)
            {
                yield return new WaitForSecondsRealtime(settings.Delay);
            }

            if (fadingImage != null)
            {
                Color c = fadingImage.color;
                c.a = (settings.FadeIn > 0f) ? 0f : 1f;
                fadingImage.color = c;
            }

            if (loadingScreen != null) loadingScreen.SetActive(true);
            UpdateLoadingText(0f);

            if (settings.FadeIn > 0f && fadingImage != null)
            {
                yield return CO_FadeImage(fadingImage, 0f, 1f, settings.FadeIn);
            }

            WhenStartLoadingAdditives?.Invoke();

            Scene bootScene = SceneManager.GetActiveScene();

            bool skipMainReload = _currentScenePair != null && pair.MainScene != null &&
                                 _currentScenePair.MainScene.GetSceneName() == pair.MainScene.GetSceneName();

            foreach (var oldGuid in _loadedAddressableAdditives.Keys.ToList())
            {
                if (!pair.AdditiveScenes.Any(scene => scene.GetSceneName() == oldGuid))
                {
                    SceneInstance instance = _loadedAddressableAdditives[oldGuid];
                    if (instance.Scene.IsValid() && instance.Scene.isLoaded)
                    {
                        yield return Addressables.UnloadSceneAsync(instance);
                    }
                    _loadedAddressableAdditives.Remove(oldGuid);
                }
            }

            settings.PostUnloadPayload?.Invoke();

            if (!skipMainReload && _hasActiveMainSceneInstance)
            {
                if (_currentMainSceneInstance.Scene.IsValid() && _currentMainSceneInstance.Scene.isLoaded)
                {
                    yield return Addressables.UnloadSceneAsync(_currentMainSceneInstance);
                }
                _hasActiveMainSceneInstance = false;
            }

            if (!skipMainReload && pair.MainScene != null)
            {
                var loadHandle = Addressables.LoadSceneAsync(pair.MainScene.AddressableReference, LoadSceneMode.Additive);
                yield return loadHandle;
                _currentMainSceneInstance = loadHandle.Result;
                _hasActiveMainSceneInstance = true;
                SceneManager.SetActiveScene(_currentMainSceneInstance.Scene);
            }

            foreach (var additive in pair.AdditiveScenes)
            {
                string guidKey = additive.GetSceneName();
                if (!_loadedAddressableAdditives.ContainsKey(guidKey))
                {
                    var loadHandle = Addressables.LoadSceneAsync(additive.AddressableReference, LoadSceneMode.Additive);
                    yield return loadHandle;
                    _loadedAddressableAdditives[guidKey] = loadHandle.Result;
                }
            }

            if (_currentScenePair == null && bootScene.IsValid() && bootScene.isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(bootScene);
            }

            UpdateLoadingText(1f);
            yield return null;

            WhenFinishedLoadingAdditives?.Invoke();
            _currentScenePair = pair;
            IsLoading = false;

            settings.Payload?.Invoke();

            if (settings.FadeOut > 0f && fadingImage != null)
            {
                yield return CO_FadeImage(fadingImage, 1f, 0f, settings.FadeOut);
            }

            if (loadingScreen != null) loadingScreen.SetActive(false);
            if (EventSystem.current != null) EventSystem.current.enabled = true;
            Application.backgroundLoadingPriority = ThreadPriority.BelowNormal;
            if (cachedEventSystem != null) cachedEventSystem.enabled = true;
        }

        private IEnumerator CO_FadeImage(Image image, float startAlpha, float targetAlpha, float duration)
        {
            float elapsed = 0f;
            Color col = image.color;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                col.a = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
                image.color = col;
                yield return null;
            }

            col.a = targetAlpha;
            image.color = col;
        }

        private void UpdateLoadingText(float progress)
        {
            if (loadingScreenText != null)
                loadingScreenText.text = $"Loading: {Mathf.RoundToInt(Mathf.Clamp01(progress) * 100f)}%";
        }
        #endregion
    }
}