using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FumoCore.Tools
{
    public class SceneLoader : MonoBehaviour
    {
        [SerializeField] private ScenePairSO startingScene;
        [SerializeField] private GameObject loadingScreen;
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
            bool isAdditive = startingScene.AdditiveScenes.Any(s =>
                string.Equals(currentName, s.GetSceneName(), StringComparison.OrdinalIgnoreCase));

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
                LoadScenePair(startingScene, null);
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
        public static void LoadScenePair(ScenePairSO pair, Action payload = null)
        {
            if (Instance != null)
            {
                if (pair == _currentScenePair)
                {
                    WhenStartLoadingAdditives?.Invoke();
                    WhenFinishedLoadingAdditives?.Invoke();
                    IsLoading = false;
                    payload?.Invoke();

                    if (Instance.loadingScreen != null)
                        Instance.loadingScreen.SetActive(false);
                    return;
                }
                Instance.StartCoroutine(Instance.CO_LoadScenePair(pair, payload));
            }
        }
        #endregion

        #region Core Coroutine
        private IEnumerator CO_LoadScenePair(ScenePairSO pair, Action payload)
        {
            if (pair == null) yield break;
            if (IsLoading) yield break;

            IsLoading = true;

            if (loadingScreen != null)
                loadingScreen.SetActive(true);
            if (loadingScreenText != null)
                loadingScreenText.text = "Loading: 0%";

            WhenStartLoadingAdditives?.Invoke();

            void UpdateLoadingText(float progress)
            {
                progress = Mathf.Clamp01(progress);
                if (loadingScreenText != null)
                    loadingScreenText.text = $"Loading: {Mathf.RoundToInt(progress * 100f)}%";
            }

            int totalOps = 1 + pair.AdditiveScenes.Count + _loadedAdditives.Count;
            int finishedOps = 0;

            IEnumerator TrackProgress(IEnumerator operation)
            {
                AsyncOperation async = null;
                while (operation.MoveNext())
                {
                    if (operation.Current is AsyncOperation op)
                        async = op;

                    float opProgress = async != null ? Mathf.Clamp01(async.progress / 0.9f) : 0f;
                    float totalProgress = (finishedOps + opProgress) / totalOps;
                    UpdateLoadingText(totalProgress);
                    yield return operation.Current;
                }

                finishedOps++;
                UpdateLoadingText((float)finishedOps / totalOps);
            }

            string currentSceneName = SceneManager.GetActiveScene().name;
            bool skipMainReload = pair.MainScene != null &&
                                  string.Equals(pair.MainScene.GetSceneName(), currentSceneName, StringComparison.OrdinalIgnoreCase);

            foreach (var oldAdditive in _loadedAdditives.ToList())
            {
                if (!pair.AdditiveScenes.Any(s => s.GetSceneName() == oldAdditive.GetSceneName()))
                {
                    if (IsSceneLoaded(oldAdditive))
                        yield return StartCoroutine(TrackProgress(UnloadScene(oldAdditive)));

                    _loadedAdditives.Remove(oldAdditive);
                }
            }

            if (_currentScenePair != null && !skipMainReload)
            {
                var oldMain = _currentScenePair.MainScene;
                if (oldMain != null && IsSceneLoaded(oldMain))
                    yield return StartCoroutine(TrackProgress(UnloadScene(oldMain)));
            }

            if (!skipMainReload && pair.MainScene != null && !IsSceneLoaded(pair.MainScene))
                yield return StartCoroutine(TrackProgress(LoadScene(pair.MainScene, true)));
            else if (skipMainReload)
                SceneManager.SetActiveScene(SceneManager.GetActiveScene());

            foreach (var additive in pair.AdditiveScenes)
            {
                if (!_loadedAdditives.Any(s => s.GetSceneName() == additive.GetSceneName()))
                {
                    yield return StartCoroutine(TrackProgress(LoadScene(additive)));
                    _loadedAdditives.Add(additive);
                }
            }
            Scene activeAtStart = SceneManager.GetSceneByName(currentSceneName);

            bool activeWasMain = pair.MainScene != null &&
                                 string.Equals(pair.MainScene.GetSceneName(), activeAtStart.name, StringComparison.OrdinalIgnoreCase);

            bool activeWasAdditive = pair.AdditiveScenes.Any(s =>
                                 string.Equals(s.GetSceneName(), activeAtStart.name, StringComparison.OrdinalIgnoreCase));

            bool originalShouldBeUnloaded = _currentScenePair == null && !activeWasMain && !activeWasAdditive && activeAtStart.isLoaded;

            if (originalShouldBeUnloaded && SceneManager.sceneCount > 1)
            {
                yield return StartCoroutine(TrackProgress(UnloadScene(activeAtStart)));
            }
            UpdateLoadingText(1f);
            yield return null;

            WhenFinishedLoadingAdditives?.Invoke();

            _currentScenePair = pair;
            IsLoading = false;
            payload?.Invoke();

            if (loadingScreen != null)
                loadingScreen.SetActive(false);
        }

        #endregion

        #region Internal Load/Unload
        private static IEnumerator LoadScene(Scene scene)
        {
            if (!scene.IsValid() || scene.isLoaded) yield break;
            AsyncOperation op = SceneManager.LoadSceneAsync(scene.name, LoadSceneMode.Additive);
            if (Instance.loadingScreen != null)
                Instance.loadingScreen.SetActive(true);
            while (!op.isDone) yield return null;
            SceneManager.SetActiveScene(scene);
        }

        private static IEnumerator UnloadScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded || SceneManager.sceneCount <= 1) yield break;
            if (SceneManager.GetActiveScene() == scene)
            {
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    Scene sc = SceneManager.GetSceneAt(i);
                    if (sc != scene && sc.isLoaded)
                    {
                        SceneManager.SetActiveScene(sc);
                        break;
                    }
                }
            }
            AsyncOperation op = SceneManager.UnloadSceneAsync(scene);
            if (Instance.loadingScreen != null)
                Instance.loadingScreen.SetActive(true);
            while (!op.isDone) yield return null;
        }
        private static IEnumerator LoadScene(SceneReference sceneRef, bool setAsActive = false)
        {
            if (sceneRef == null) yield break;

            string name = sceneRef.GetSceneName();
            if (Instance.loadingScreen != null)
                Instance.loadingScreen.SetActive(true);

            AsyncOperation op = SceneManager.LoadSceneAsync(name, LoadSceneMode.Additive);
            if (op == null)
            {
                yield break;
            }
            while (!op.isDone) yield return null;

            Scene scene = SceneManager.GetSceneByName(name);

            if (setAsActive) SceneManager.SetActiveScene(scene);

            yield return null;
        }
        private static IEnumerator UnloadScene(SceneReference sceneRef)
        {
            if (sceneRef == null) yield break;

            string name = sceneRef.GetSceneName();
            Scene s = SceneManager.GetSceneByName(name);

            if (!s.IsValid() || !s.isLoaded)
                yield break;

            if (SceneManager.sceneCount <= 1)
                yield break;

            if (SceneManager.GetActiveScene() == s)
            {
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    Scene sc = SceneManager.GetSceneAt(i);
                    if (sc != s && sc.isLoaded)
                    {
                        SceneManager.SetActiveScene(sc);
                        break;
                    }
                }
            }

            yield return SceneManager.UnloadSceneAsync(name);
            yield return Resources.UnloadUnusedAssets();
            System.GC.Collect();
        }

        private static bool IsSceneLoaded(SceneReference sceneRef)
        {
            if (sceneRef == null) return false;
            Scene s = SceneManager.GetSceneByName(sceneRef.GetSceneName());
            return s.IsValid() && s.isLoaded;
        }
        #endregion
    }
}
