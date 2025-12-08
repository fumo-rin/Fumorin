using RinCore;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RinCore
{
    #region Singleton & Setup
    public partial class TimeSlowHandler : MonoBehaviour
    {
        private static TimeSlowHandler instance;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            if (timeScaleTable != null)
            {
                timeScaleTable.Clear();
            }
            slowdowns.Clear();
            Time.maximumDeltaTime = 1f / 60f;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 120;
        }

        [Initialize(-50000)]
        private static void Reinitialize()
        {
            TimescaleMultiplier = 1f;
            timeScaleTable = new Dictionary<string, float>();
            slowdowns.Clear();
            simulatedSlowCurrent = 1f;
            simulatedSlowTarget = 1f;
        }
    }
    #endregion

    #region Properties
    public partial class TimeSlowHandler
    {
        public static float TimescaleMultiplier { get; private set; } = 1f;
        public static float SimulatedSlowdown { get; private set; } = 1f;

        private static Dictionary<string, float> timeScaleTable = new();
    }
    #endregion

    #region Slowdown System
    public partial class TimeSlowHandler
    {
        [Serializable]
        public class SlowdownEntry
        {
            public string key;
            public float current;
            public float target;
            public float durationRemaining;
            public float easeDuration;

            public SlowdownEntry(string key, float target, float duration, float easeDuration = 0.35f)
            {
                this.key = key;
                this.target = target;
                this.durationRemaining = duration;
                this.easeDuration = easeDuration;
                this.current = 1f;
            }

            public void Update(float deltaTime)
            {
                current = Mathf.Lerp(current, target, Mathf.Clamp01(deltaTime / easeDuration));
                if (durationRemaining > 0f)
                    durationRemaining -= deltaTime;
            }

            public bool Expired => durationRemaining <= 0f;
        }

        private static Dictionary<string, SlowdownEntry> slowdowns = new();
        private static HashSet<string> toRemove = new();
    }
    #endregion

    #region Public Methods
    public partial class TimeSlowHandler
    {
        public static void SetTimescale(float value) => TimescaleMultiplier = Mathf.Max(0f, value);
        public static void AddSlow(string key, float amount, float duration, float easeDuration = 0.35f)
        {
            TryCreateInstance();
            if (slowdowns.TryGetValue(key, out var entry))
            {
                entry.target = amount;
                entry.durationRemaining = Mathf.Max(entry.durationRemaining, duration);
                entry.easeDuration = easeDuration;
            }
            else
            {
                slowdowns[key] = new SlowdownEntry(key, amount, duration, easeDuration);
            }
        }

        public static void CombineSlow(string key, float amount, float duration, float easeDuration = 0.35f)
        {
            TryCreateInstance();
            if (slowdowns.TryGetValue(key, out var existing))
            {
                if (amount < existing.target) existing.target = amount;
                existing.durationRemaining = Mathf.Max(existing.durationRemaining, duration);
                existing.easeDuration = easeDuration;
            }
            else
            {
                slowdowns[key] = new SlowdownEntry(key, amount, duration, easeDuration);
            }
        }

        public static void RemoveSlow(string key) => slowdowns.Remove(key);
        public static bool HasSlow(string key) => slowdowns.ContainsKey(key);
        public static void SetSimulatedSlowdownTarget(float target)
        {
            TryCreateInstance();
            simulatedSlowTarget = Mathf.Clamp01(target);
        }
        public static void AddDurationlessTimescale(string key, float value)
        {
            TryCreateInstance();
            timeScaleTable[key] = value;
        }

        public static void RemoveDurationlessTimescale(string key)
        {
            timeScaleTable.Remove(key);
        }

        public static void Reload()
        {
            TryCreateInstance();
            slowdowns.Clear();
            timeScaleTable.Clear();
            simulatedSlowCurrent = 1f;
            simulatedSlowTarget = 1f;
        }
    }
    #endregion

    #region Update Loop
    public partial class TimeSlowHandler
    {
        private const float DefaultEaseOut = 0.35f;
        private const float FadeInDuration = 0.1f;

        private static float simulatedSlowCurrent = 1f;
        private static float simulatedSlowTarget = 1f;
        private void LateUpdate()
        {
            float deltaTime = Time.unscaledDeltaTime;

            if (GeneralManager.IsPaused)
            {
                Time.timeScale = 0f;
                Time.fixedDeltaTime = 0f;
                return;
            }
            toRemove.Clear();
            foreach (var kvp in slowdowns)
            {
                kvp.Value.Update(deltaTime);
                if (kvp.Value.Expired) toRemove.Add(kvp.Key);
            }
            foreach (var key in toRemove) slowdowns.Remove(key);

            simulatedSlowCurrent = Mathf.Lerp(simulatedSlowCurrent, simulatedSlowTarget, Mathf.Clamp01(deltaTime / FadeInDuration));

            float totalSlow = simulatedSlowCurrent;
            foreach (var entry in slowdowns.Values)
                totalSlow *= entry.current;

            SimulatedSlowdown = totalSlow;

            Time.timeScale = CalculateFinalTimescale();
            Time.fixedDeltaTime = 0.01667f * Time.timeScale;
        }
    }
    #endregion

    #region Internal Calculations
    public partial class TimeSlowHandler
    {
        private static void TryCreateInstance()
        {
            if (instance == null || instance.gameObject == null)
            {
                GameObject go = new GameObject("TimeSlowHandler");
                instance = go.AddComponent<TimeSlowHandler>();
                slowdowns.Clear();
                timeScaleTable.Clear();
                simulatedSlowCurrent = 1f;
                simulatedSlowTarget = 1f;
                toRemove = new HashSet<string>();
                Time.maximumDeltaTime = 1f / 60f;
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = 120;
            }
        }
        private float CalculateFinalTimescale()
        {
            float final = 1f * TimescaleMultiplier * SimulatedSlowdown;

            foreach (var kvp in timeScaleTable)
                final *= kvp.Value;

            final *= GeneralManager.IsPaused.AsFloat(0f, 1f);
            final *= SceneLoader.IsLoading.AsFloat(0f, 1f);

            return Mathf.Max(0f, final);
        }
    }
    #endregion
}