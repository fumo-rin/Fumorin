using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace RinCore
{
    public static class GlobalCoroutineRunnerExtensions
    {
        public static Coroutine RunRoutine(this IEnumerator co, string? keyOverride = null, bool persistAcrossScenes = false)
        {
            return GlobalCoroutineRunner.StartRoutine(keyOverride ?? nameof(co), co, persistAcrossScenes);
        }
    }
    public class GlobalCoroutineRunner : MonoBehaviour
    {
        private static GlobalCoroutineRunner _instance;

        private static readonly Dictionary<string, List<Coroutine>> trackedCoroutines
            = new Dictionary<string, List<Coroutine>>();

        private static GlobalCoroutineRunner Instance
        {
            get
            {
                if (_instance == null)
                {
                    trackedCoroutines.Clear();
                    GameObject go = new GameObject("GlobalCoroutineRunner");
                    _instance = go.AddComponent<GlobalCoroutineRunner>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }

        private void OnEnable()
        {
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
        }

        private void OnSceneUnloaded(Scene scene)
        {
            StopAllSceneCoroutines(scene.name);
        }

        private static string ResolveKey(string key, bool persistAcrossScenes)
        {
            return persistAcrossScenes ? key : $"{key}_{SceneManager.GetActiveScene().name}";
        }

        public static Coroutine StartRoutine(string key, IEnumerator routine, bool persistAcrossScenes = false)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Coroutine key cannot be null or empty", nameof(key));

            string trackedKey = ResolveKey(key, persistAcrossScenes);

            Coroutine c = Instance.StartCoroutine(routine);

            if (!trackedCoroutines.TryGetValue(trackedKey, out var list))
            {
                list = new List<Coroutine>();
                trackedCoroutines[trackedKey] = list;
            }

            list.Add(c);
            CleanupKey(trackedKey);

            return c;
        }

        public static void StopCoroutine(string key, Coroutine coroutine, bool persistAcrossScenes = false)
        {
            if (coroutine == null || string.IsNullOrEmpty(key)) return;

            string trackedKey = ResolveKey(key, persistAcrossScenes);

            if (!trackedCoroutines.TryGetValue(trackedKey, out var list))
                return;

            if (list.Contains(coroutine))
            {
                Instance.StopCoroutine(coroutine);
                list.Remove(coroutine);
            }

            CleanupKey(trackedKey);
        }
        public static void StopAllOfKey(string key)
        {
            if (string.IsNullOrEmpty(key)) return;

            var allKeys = new List<string>(trackedCoroutines.Keys);
            var keysToRemove = new List<string>();

            foreach (var dictKey in allKeys)
            {
                if (dictKey == key || dictKey.StartsWith(key + "_"))
                {
                    if (trackedCoroutines.TryGetValue(dictKey, out var coroutineList))
                    {
                        foreach (var c in coroutineList)
                        {
                            if (c != null)
                                Instance.StopCoroutine(c);
                        }
                        keysToRemove.Add(dictKey);
                    }
                }
            }

            foreach (var k in keysToRemove)
                trackedCoroutines.Remove(k);
        }
        public static void StopAll()
        {
            foreach (var list in trackedCoroutines.Values)
            {
                foreach (var c in list)
                {
                    if (c != null)
                        Instance.StopCoroutine(c);
                }
            }
            trackedCoroutines.Clear();
            Instance.StopAllCoroutines();
        }

        public static void StopAllSceneCoroutines(string sceneName = null)
        {
            if (sceneName == null)
                sceneName = SceneManager.GetActiveScene().name;

            var keysToRemove = new List<string>();

            foreach (var kvp in trackedCoroutines)
            {
                if (kvp.Key.EndsWith("_" + sceneName))
                {
                    foreach (var c in kvp.Value)
                    {
                        if (c != null)
                            Instance.StopCoroutine(c);
                    }
                    keysToRemove.Add(kvp.Key);
                }
            }

            foreach (var k in keysToRemove)
                trackedCoroutines.Remove(k);
        }

        private static void CleanupKey(string key)
        {
            if (trackedCoroutines.TryGetValue(key, out var list))
            {
                list.RemoveAll(c => c == null);
                if (list.Count == 0)
                    trackedCoroutines.Remove(key);
            }
        }
    }

}
