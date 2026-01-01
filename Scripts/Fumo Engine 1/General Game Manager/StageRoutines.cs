using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RinCore
{
    /// <summary>
    /// Scene-bound runner for stage routines.
    /// Automatically creates a new instance if none exists.
    /// Cleared on destroy.
    /// </summary>

    [DefaultExecutionOrder(-10000)]
    public class StageRoutines : MonoBehaviour
    {
        private static StageRoutines instance;
        private static bool debugMode = false;

        private Dictionary<string, List<Coroutine>> routines;

        private void Initialize()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            routines = new Dictionary<string, List<Coroutine>>();
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                StopAll();
                instance = null;
                if (debugMode)
                    Debug.Log("[StageRoutines] Instance destroyed and cleared.");
            }
        }
        public static void StopAll()
        {
            if (instance != null)
            {
                instance.StopAllCoroutines();
                instance.routines.Clear();
            }
        }
        private static StageRoutines EnsureInstance()
        {
            if (instance == null)
            {
                GameObject go = new GameObject("Stage Routines Runner");
                instance = go.AddComponent<StageRoutines>();
                instance.Initialize();
            }
            return instance;
        }
        public static Coroutine StartRoutine(string key, IEnumerator routine, bool stopSameKey = false)
        {
            var inst = EnsureInstance();
            if (stopSameKey) StopRoutine(key);

            if (!inst.routines.ContainsKey(key))
                inst.routines[key] = new List<Coroutine>();

            Coroutine c = inst.StartCoroutine(routine);
            inst.routines[key].Add(c);

            if (debugMode)
                Debug.Log($"[StageRoutines] Started routine: {key}");

            return c;
        }
        public static void StopRoutine(string key)
        {
            var inst = EnsureInstance();
            if (inst.routines.TryGetValue(key, out List<Coroutine> coros))
            {
                if (coros != null)
                {
                    foreach (var c in coros)
                    {
                        if (c != null)
                            inst.StopCoroutine(c);
                    }
                    coros.Clear();
                    if (debugMode)
                        Debug.Log($"[StageRoutines] Stopped routines: {key}");
                }
            }
        }
    }
}
