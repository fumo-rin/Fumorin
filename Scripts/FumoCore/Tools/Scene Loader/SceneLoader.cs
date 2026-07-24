using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace rinCore
{
    public class SceneLoader : MonoBehaviour
    {
        [SerializeField] private ScenePackSO scenePack;
        [SerializeField] private GameObject loadingScreen;
        [SerializeField] private Image fadingImage;
        [SerializeField] private TMP_Text loadingScreenText;

        internal static SceneLoader Instance { get; private set; }

        private static ScenePairSO _currentScenePair;
        private static HashSet<string> _loadedAdditiveSceneNames = new();
        private static string _currentMainSceneName = string.Empty;

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
            if (Instance != this || scenePack == null)
                yield break;

            yield return StartCoroutine(CO_EnsureUnloadPreventionSceneLoaded());

            if (scenePack.EditorStartingScene != null)
            {
                yield return null;
#if UNITY_EDITOR
                LoadScenePair(scenePack.EditorStartingScene, new SceneLoadSettings { Payload = null, Delay = 0f });
#else
                if (scenePack.LoadStartingSceneInBuild && scenePack.BuildStartingScene != null)
                    LoadScenePair(scenePack.BuildStartingScene, new SceneLoadSettings { Payload = null, Delay = 5f });
#endif
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ResetStatics()
        {
            Instance = null;
            _currentScenePair = null;
            _loadedAdditiveSceneNames = new HashSet<string>();
            _currentMainSceneName = string.Empty;
            IsLoading = false;
        }

        private IEnumerator CO_EnsureUnloadPreventionSceneLoaded()
        {
            if (scenePack == null || scenePack.UnloadPreventionScene == null || !scenePack.UnloadPreventionScene.IsValid)
                yield break;

            string sceneName = scenePack.UnloadPreventionScene.GetSceneName();
            Scene sc = SceneManager.GetSceneByName(sceneName);

            if (!sc.IsValid() || !sc.isLoaded)
            {
                AsyncOperation loadOp = SceneManager.LoadSceneAsync(scenePack.UnloadPreventionScene.ScenePath, LoadSceneMode.Additive);
                if (loadOp != null)
                {
                    yield return loadOp;
                }
            }
        }

        #region Public Wrapper
        public struct SceneLoadSettings
        {
            public Action Payload, PostUnloadPayload;
            public float Delay;
            public float FadeIn, FadeOut;
            public bool ForceReload;
        }

        public static void MainMenu(SceneLoader.SceneLoadSettings? settings = null)
        {
            if (Instance != null && Instance.scenePack != null && Instance.scenePack.EditorStartingScene is ScenePairSO p)
                LoadScenePair(p, settings);
        }

        public static void LoadScenePair(ScenePairSO pair, SceneLoadSettings? settings = null)
        {
            SceneLoadSettings finalSettings = settings ?? new SceneLoadSettings()
            {
                Delay = 0f,
                FadeIn = 0.1f,
                FadeOut = 0.1f,
                ForceReload = false
            };

            if (Instance != null && !IsLoading)
            {
                if (pair == _currentScenePair && !finalSettings.ForceReload)
                {
                    if (finalSettings.Delay <= 0f)
                    {
                        WhenStartLoadingAdditives?.Invoke();
                        WhenFinishedLoadingAdditives?.Invoke();
                        finalSettings.Payload?.Invoke();
                    }
                    else
                    {
                        IEnumerator CO_RunPayloads(float delay)
                        {
                            yield return new WaitForSeconds(delay);
                            WhenStartLoadingAdditives?.Invoke();
                            WhenFinishedLoadingAdditives?.Invoke();
                            finalSettings.Payload?.Invoke();
                        }
                        if (Instance is SceneLoader s)
                        {
                            s.StartCoroutine(CO_RunPayloads(finalSettings.Delay));
                        }
                    }
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

            yield return CO_EnsureUnloadPreventionSceneLoaded();

            WhenStartLoadingAdditives?.Invoke();

            Scene bootScene = SceneManager.GetActiveScene();
            string persistentName = (scenePack != null && scenePack.UnloadPreventionScene != null && scenePack.UnloadPreventionScene.IsValid)
                ? scenePack.UnloadPreventionScene.GetSceneName()
                : string.Empty;

            string newMainSceneName = pair.MainScene != null ? pair.MainScene.GetSceneName() : string.Empty;
            if (string.IsNullOrEmpty(_currentMainSceneName) && bootScene.IsValid())
            {
                _currentMainSceneName = bootScene.name;
            }
            bool skipMainReload = !settings.ForceReload
                && !string.IsNullOrEmpty(_currentMainSceneName)
                && _currentMainSceneName == newMainSceneName;

            List<string> oldAdditives = _loadedAdditiveSceneNames.ToList();
            foreach (var oldName in oldAdditives)
            {
                if (!string.IsNullOrEmpty(persistentName) && oldName == persistentName)
                    continue;

                bool keptInNew = !settings.ForceReload && pair.AdditiveScenes.Any(s => s.GetSceneName() == oldName);
                if (!keptInNew)
                {
                    Scene sc = SceneManager.GetSceneByName(oldName);
                    if (sc.IsValid() && sc.isLoaded)
                    {
                        AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(sc);
                        if (unloadOp != null) yield return unloadOp;
                    }
                    _loadedAdditiveSceneNames.Remove(oldName);
                }
            }

            settings.PostUnloadPayload?.Invoke();

            if (!skipMainReload && !string.IsNullOrEmpty(_currentMainSceneName))
            {
                if (string.IsNullOrEmpty(persistentName) || _currentMainSceneName != persistentName)
                {
                    Scene oldMain = SceneManager.GetSceneByName(_currentMainSceneName);
                    if (oldMain.IsValid() && oldMain.isLoaded)
                    {
                        AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(oldMain);
                        if (unloadOp != null) yield return unloadOp;
                    }
                }
                _currentMainSceneName = string.Empty;
            }

            if (!skipMainReload && pair.MainScene != null && pair.MainScene.IsValid)
            {
                string sceneToLoad = pair.MainScene.ScenePath;
                AsyncOperation loadOp = SceneManager.LoadSceneAsync(sceneToLoad, LoadSceneMode.Additive);

                while (!loadOp.isDone)
                {
                    UpdateLoadingText(loadOp.progress * 0.5f);
                    yield return null;
                }

                _currentMainSceneName = pair.MainScene.GetSceneName();
                Scene newMain = SceneManager.GetSceneByName(_currentMainSceneName);
                if (newMain.IsValid())
                {
                    SceneManager.SetActiveScene(newMain);
                }
            }

            int totalAdditives = pair.AdditiveScenes.Count;
            int loadedCount = 0;

            foreach (var additive in pair.AdditiveScenes)
            {
                if (additive != null && additive.IsValid)
                {
                    string addName = additive.GetSceneName();
                    if (!_loadedAdditiveSceneNames.Contains(addName))
                    {
                        AsyncOperation addOp = SceneManager.LoadSceneAsync(additive.ScenePath, LoadSceneMode.Additive);
                        while (!addOp.isDone)
                        {
                            float baseProg = 0.5f;
                            float addProg = ((float)loadedCount / Mathf.Max(1, totalAdditives)) * 0.5f;
                            UpdateLoadingText(baseProg + addProg);
                            yield return null;
                        }
                        _loadedAdditiveSceneNames.Add(addName);
                    }
                }
                loadedCount++;
            }

            if (_currentScenePair == null && bootScene.IsValid() && bootScene.isLoaded &&
                bootScene.name != _currentMainSceneName && bootScene.name != persistentName)
            {
                AsyncOperation bootUnload = SceneManager.UnloadSceneAsync(bootScene);
                if (bootUnload != null) yield return bootUnload;
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