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
        [SerializeField] private ScenePairSO startingScene;
        [SerializeField] private GameObject loadingScreen;
        [SerializeField] private Image fadingImage;
        [SerializeField] private TMP_Text loadingScreenText;

        internal static SceneLoader Instance { get; private set; }

        private static ScenePairSO _currentScenePair;
        private static HashSet<SceneReference> _loadedAdditives = new();

        public static bool IsLoading { get; private set; }
        public static string CurrentSceneName => SceneManager.GetActiveScene().name;

        public static event Action WhenStartLoadingAdditives;
        public static event Action WhenFinishedLoadingAdditives;
        private Scene _initialScene;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                if (loadingScreen != null)
                    loadingScreen.SetActive(false);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            _initialScene = SceneManager.GetActiveScene();

            if (startingScene == null)
                return;

            string currentName = _initialScene.name;
            string mainName = startingScene.MainScene != null ? startingScene.MainScene.GetSceneName() : string.Empty;
            bool isMain = string.Equals(currentName, mainName, StringComparison.OrdinalIgnoreCase);
            bool isAdditive = startingScene.AdditiveScenes.Any(s => string.Equals(currentName, s.GetSceneName(), StringComparison.OrdinalIgnoreCase));

            if (isMain)
            {
                _currentScenePair = startingScene;
                foreach (var additive in startingScene.AdditiveScenes)
                {
                    if (!_loadedAdditives.Any(s => s.GetSceneName() == additive.GetSceneName()))
                    {
                        StartCoroutine(LoadScene(additive));
                        _loadedAdditives.Add(additive);
                    }
                }
                if (loadingScreen != null) loadingScreen.SetActive(false);
            }
            else if (isAdditive)
            {
                _currentScenePair = startingScene;

                var existingAdditiveRef = startingScene.AdditiveScenes.FirstOrDefault(s =>
                    string.Equals(s.GetSceneName(), currentName, StringComparison.OrdinalIgnoreCase));

                if (existingAdditiveRef != null)
                    _loadedAdditives.Add(existingAdditiveRef);

                if (startingScene.MainScene != null)
                    StartCoroutine(LoadScene(startingScene.MainScene, true));

                foreach (var additive in startingScene.AdditiveScenes)
                {
                    if (!string.Equals(additive.GetSceneName(), currentName, StringComparison.OrdinalIgnoreCase) &&
                        !_loadedAdditives.Any(s => s.GetSceneName() == additive.GetSceneName()))
                    {
                        StartCoroutine(LoadScene(additive));
                        _loadedAdditives.Add(additive);
                    }
                }

                if (loadingScreen != null) loadingScreen.SetActive(false);
            }
            else
            {
                LoadScenePair(startingScene, new()
                {
                    Payload = null
                });
            }
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ResetStatics()
        {
            Instance = null;
            _currentScenePair = null;
            _loadedAdditives = new HashSet<SceneReference>();
            IsLoading = false;
        }
        #region Public Wrapper
        public struct SceneLoadSettings
        {
            public Action Payload;
            public float Delay;
        }
        public static void LoadScenePair(ScenePairSO pair, SceneLoadSettings? settings = null)
        {
            SceneLoadSettings finalSettings = settings ?? new();
            if (Instance != null)
            {
                if (pair == _currentScenePair)
                {
                    WhenStartLoadingAdditives?.Invoke();
                    WhenFinishedLoadingAdditives?.Invoke();
                    IsLoading = false;
                    finalSettings.Payload?.Invoke();

                    if (Instance.loadingScreen != null)
                        Instance.loadingScreen.SetActive(false);
                    return;
                }
                Instance.StartCoroutine(Instance.CO_LoadScenePair(pair, finalSettings.Payload, finalSettings.Delay));
            }
        }
        #endregion

        #region Core Coroutine
        private IEnumerator CO_LoadScenePair(ScenePairSO pair, Action payload, float delay)
        {
            if (pair == null || IsLoading) yield break;
            IsLoading = true;

            EventSystem s = EventSystem.current;
            if (s != null) s.enabled = false;

            if (loadingScreen != null) loadingScreen.SetActive(true);
            UpdateLoadingText(0f);

            Image loadBackground = fadingImage;
            if (delay > 0f)
            {
                float remainingDelay = delay;
                if (loadBackground == null)
                {
                    yield return new WaitForSecondsRealtime(delay);
                }
                else
                {
                    loadBackground.color = loadBackground.color.Opacity(0);
                    while (remainingDelay > 0f && loadBackground != null)
                    {
                        float lerp01 = 1f - (remainingDelay / delay.Max(0.0001f));
                        lerp01 = lerp01.Clamp(0f, 1f);
                        byte opacity = lerp01.MapFrom01(0f, 255f).Floor().ToByte();
                        loadBackground.color = loadBackground.color.Opacity(opacity);
                        remainingDelay -= Time.unscaledDeltaTime;
                        yield return null;
                    }
                }
            }
            if (loadBackground != null) loadBackground.color = loadBackground.color.Opacity(255);

            WhenStartLoadingAdditives?.Invoke();

            string currentSceneName = SceneManager.GetActiveScene().name;
            bool skipMainReload = pair.MainScene != null && string.Equals(pair.MainScene.GetSceneName(), currentSceneName, StringComparison.OrdinalIgnoreCase);

            int totalOps = 1 + pair.AdditiveScenes.Count + _loadedAdditives.Count;
            int finishedOps = 0;

            IEnumerator WaitForAsyncOp(AsyncOperation op)
            {
                if (op == null) yield break;
                while (!op.isDone)
                {
                    float opProgress = Mathf.Clamp01(op.progress / 0.9f);
                    float totalProgress = (finishedOps + opProgress) / totalOps;
                    UpdateLoadingText(totalProgress);
                    yield return null;
                }
                finishedOps++;
                UpdateLoadingText((float)finishedOps / totalOps);
            }
            foreach (var oldAdditive in _loadedAdditives.ToList())
            {
                if (!pair.AdditiveScenes.Any(s => s.GetSceneName() == oldAdditive.GetSceneName()))
                {
                    Scene sceneObj = SceneManager.GetSceneByName(oldAdditive.GetSceneName());
                    if (sceneObj.IsValid() && sceneObj.isLoaded)
                    {
                        if (SceneManager.GetActiveScene() == sceneObj && SceneManager.sceneCount > 1)
                        {
                            for (int i = 0; i < SceneManager.sceneCount; i++)
                            {
                                Scene sc = SceneManager.GetSceneAt(i);
                                if (sc != sceneObj && sc.isLoaded)
                                {
                                    SceneManager.SetActiveScene(sc);
                                    break;
                                }
                            }
                        }

                        yield return StartCoroutine(WaitForAsyncOp(SceneManager.UnloadSceneAsync(sceneObj)));
                    }
                    _loadedAdditives.Remove(oldAdditive);
                }
            }
            if (_currentScenePair != null && !skipMainReload)
            {
                var oldMain = _currentScenePair.MainScene;
                if (oldMain != null)
                {
                    Scene sceneObj = SceneManager.GetSceneByName(oldMain.GetSceneName());
                    if (sceneObj.IsValid() && sceneObj.isLoaded)
                    {
                        if (SceneManager.GetActiveScene() == sceneObj && SceneManager.sceneCount > 1)
                        {
                            for (int i = 0; i < SceneManager.sceneCount; i++)
                            {
                                Scene sc = SceneManager.GetSceneAt(i);
                                if (sc != sceneObj && sc.isLoaded)
                                {
                                    SceneManager.SetActiveScene(sc);
                                    break;
                                }
                            }
                        }

                        yield return StartCoroutine(WaitForAsyncOp(SceneManager.UnloadSceneAsync(sceneObj)));
                    }
                }
            }
            if (!skipMainReload && pair.MainScene != null)
            {
                string mainName = pair.MainScene.GetSceneName();
                Scene sceneObj = SceneManager.GetSceneByName(mainName);

                if (!sceneObj.IsValid() || !sceneObj.isLoaded)
                {
                    AsyncOperation op = SceneManager.LoadSceneAsync(mainName, LoadSceneMode.Additive);
                    yield return StartCoroutine(WaitForAsyncOp(op));

                    Scene newlyLoadedMain = SceneManager.GetSceneByName(mainName);
                    if (newlyLoadedMain.IsValid() && newlyLoadedMain.isLoaded)
                    {
                        SceneManager.SetActiveScene(newlyLoadedMain);
                    }
                }
            }
            else if (skipMainReload)
            {
                SceneManager.SetActiveScene(SceneManager.GetActiveScene());
            }
            foreach (var additive in pair.AdditiveScenes)
            {
                if (!_loadedAdditives.Any(s => s.GetSceneName() == additive.GetSceneName()))
                {
                    string additiveName = additive.GetSceneName();
                    AsyncOperation op = SceneManager.LoadSceneAsync(additiveName, LoadSceneMode.Additive);
                    yield return StartCoroutine(WaitForAsyncOp(op));

                    _loadedAdditives.Add(additive);
                }
            }
            Scene activeAtStart = SceneManager.GetSceneByName(currentSceneName);
            bool activeWasMain = pair.MainScene != null && string.Equals(pair.MainScene.GetSceneName(), activeAtStart.name, StringComparison.OrdinalIgnoreCase);
            bool activeWasAdditive = pair.AdditiveScenes.Any(s => string.Equals(s.GetSceneName(), activeAtStart.name, StringComparison.OrdinalIgnoreCase));
            bool originalShouldBeUnloaded = _currentScenePair == null && !activeWasMain && !activeWasAdditive && activeAtStart.isLoaded;

            if (originalShouldBeUnloaded && SceneManager.sceneCount > 1)
            {
                if (SceneManager.GetActiveScene() == activeAtStart)
                {
                    for (int i = 0; i < SceneManager.sceneCount; i++)
                    {
                        Scene sc = SceneManager.GetSceneAt(i);
                        if (sc != activeAtStart && sc.isLoaded)
                        {
                            SceneManager.SetActiveScene(sc);
                            break;
                        }
                    }
                }
                yield return StartCoroutine(WaitForAsyncOp(SceneManager.UnloadSceneAsync(activeAtStart)));
            }
            UpdateLoadingText(1f);
            yield return null;
            //yield return Resources.UnloadUnusedAssets();
            //GC.Collect();

            WhenFinishedLoadingAdditives?.Invoke();

            _currentScenePair = pair;
            IsLoading = false;
            payload?.Invoke();

            if (loadingScreen != null) loadingScreen.SetActive(false);
            if (s != null) s.enabled = true;
        }

        private void UpdateLoadingText(float progress)
        {
            progress = Mathf.Clamp01(progress);
            if (loadingScreenText != null)
                loadingScreenText.text = $"Loading: {Mathf.RoundToInt(progress * 100f)}%";
        }
        #endregion

        #region Internal Load/Unload (Legacy Callbacks fallback)
        private static IEnumerator LoadScene(Scene scene)
        {
            if (!scene.IsValid() || scene.isLoaded) yield break;
            AsyncOperation op = SceneManager.LoadSceneAsync(scene.name, LoadSceneMode.Additive);
            while (!op.isDone) yield return null;
            SceneManager.SetActiveScene(scene);
        }
        private static IEnumerator LoadScene(SceneReference sceneRef, bool setAsActive = false)
        {
            if (sceneRef == null) yield break;
            string name = sceneRef.GetSceneName();
            AsyncOperation op = SceneManager.LoadSceneAsync(name, LoadSceneMode.Additive);
            while (!op.isDone) yield return null;
            if (setAsActive) SceneManager.SetActiveScene(SceneManager.GetSceneByName(name));
        }
        #endregion
    }
}