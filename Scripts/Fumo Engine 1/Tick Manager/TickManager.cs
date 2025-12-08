using RinCore;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RinCore
{
    #region Attack Tick
    public partial class TickManager
    {
        public static bool WasAttackTickThisFrame;
        public FloatSO attackTickSO;
        float attackTickDuration = 0.05f;
        float lastAttackTickTime;
        public static float AttackTickLength => instance == null ? 0.05f : instance.attackTickSO == null ? (instance.attackTickDuration) : instance.attackTickSO;
        public void RunAttackTick(float time)
        {
            WasAttackTickThisFrame = false;
            if (time > lastAttackTickTime + AttackTickLength)
            {
                WasAttackTickThisFrame = true;
                lastAttackTickTime = time;
                AttackTickLightweight?.Invoke();
            }
        }
    }
    #endregion
    #region Main Tick
    public partial class TickManager
    {
        public static bool WasMainTickThisFrame;
        public static event GameTick MainTick;
        public static event GameTickLightweight MainTickLightweight;
        public static event GameTickLightweight AttackTickLightweight;
        float lastMainTickTime;
        static int tickCount = 0;
        public static int CurrentTick => tickCount;
        [Range(0.02f, 1f)]
        [SerializeField] float mainTickDuration = 0.5f;
        [SerializeField] FloatSO mainTickSO;
        public static float TickLength => instance == null ? 0.5f : instance.mainTickSO == null ? (instance.mainTickDuration) : instance.mainTickSO;
        private void RunMainTick(float time)
        {
            WasMainTickThisFrame = false;
            if (time > lastMainTickTime + TickLength)
            {
                WasMainTickThisFrame = true;
                tickCount++;
                MainTick?.Invoke(tickCount, time - lastMainTickTime);
                MainTickLightweight?.Invoke();
                lastMainTickTime = time;
            }
        }
    }
    #region AI Think
    public partial class TickManager
    {
        float lastAITickTime;
        public static GameTickLightweight AIThinkTick;
        [SerializeField] FloatSO AITickTime;
        public static float AITickDuration => instance.AITickTime == null ? 0.15f : instance.AITickTime;
        public static bool WasAIThinkTickThisFrame;
        public void RunAIThinkTick(float time)
        {
            WasAIThinkTickThisFrame = false;
            if (time > lastAITickTime + (AITickTime == null ? 0.2f : AITickTime))
            {
                WasAIThinkTickThisFrame = true;
                lastAITickTime = time;
                AIThinkTick?.Invoke();
            }
        }
    }
    #endregion
    #endregion
    #region Heartbeat
    public partial class TickManager
    {
        public static event GameTickLightweight Heartbeat;
        private int lastHeartbeat;
        private void RunHeartbeat()
        {
            int currentTime = (int)Time.time;
            if (Mathf.Abs(lastHeartbeat - currentTime) > 5)
            {
                lastHeartbeat = currentTime;
            }
            int ticksMissed = currentTime - lastHeartbeat;
            if (ticksMissed > 0)
            {
                lastHeartbeat = currentTime;
                Heartbeat?.Invoke();
            }
        }
    }
    #endregion
    public partial class TickManager : MonoBehaviour
    {
        static TickManager instance;
        public delegate void GameTick(int tick, float tickDeltaTime);
        public delegate void GameTickLightweight();
        private void Awake()
        {
            if (instance != null)
            {
                Destroy(gameObject);
                return;
            }
            transform.SetParent(null);
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        private void Update()
        {
            if (instance != this)
            {
                return;
            }
            RunMainTick(Time.time);
            RunAttackTick(Time.time);
            RunAIThinkTick(Time.time);
            RunHeartbeat();
        }
        private void OnValidate()
        {
            if (mainTickSO != null) mainTickDuration = mainTickSO;
        }
    }
}
