using rinCore;
using System.Collections;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using System;
using UnityEditor;

namespace rinCore
{
    #region Funny Explosion
    public partial class GeneralManager
    {
        [SerializeField] GameObject funnyExplosion;
        [SerializeField] ParticleSystem Explosion3D;
        [SerializeField] ACWrapper funnyExplosionSound;
        public static void FunnyExplosion(Vector2 position, float scale = 1f)
        {
            GameObject x = Instantiate(Instance.funnyExplosion, position, Quaternion.identity);
            Destroy(x, 1.02f);
            x.transform.localScale *= scale;
            x.transform.localScale = new(x.transform.localScale.x.Max(0.25f), x.transform.localScale.y.Max(0.25f), 1);
            Instance.funnyExplosionSound.Play(position);
        }
        public struct explosionPacket
        {
            public Vector3 position;
            public bool is3d;
            public float scale;
            public bool playSound;
            public explosionPacket(Vector3 position, bool is3d = false)
            {
                this.position = position;
                this.is3d = is3d;
                this.scale = 1f;
                this.playSound = true;
            }
        }
        public static void FunnyExplosion(explosionPacket packet)
        {
            if (!packet.is3d)
            {
                FunnyExplosion((Vector2)packet.position, packet.scale);
            }
            else
            {
                if (Instance is GeneralManager g && g.gameObject != null && g.gameObject.activeInHierarchy)
                {
                    g.Explosion3D.EmitSingleParticleCached(packet.position, null, 0f, null, packet.scale);
                    if (packet.playSound) g.funnyExplosionSound.Play(packet.position);
                }
            }
        }
    }
    #endregion
    #region Pause
    public partial class GeneralManager
    {
        public delegate void PauseToggle(bool state);
        public static event PauseToggle WhenPauseToggle;
        public delegate bool FreezePauseAbility();
        public static event FreezePauseAbility BlockTogglePause;
        public static bool IsPaused { get; private set; }
        public static void SetPause(bool state)
        {
            IsPaused = state;
            if (state)
            {
                IsPaused = true;
            }
            else
            {
                IsPaused = false;
            }
            WhenPauseToggle?.Invoke(state);
        }
        [QFSW.QC.Command("-Pause")]
        public static void PauseGame()
        {
            SetPause(true);
        }
        [QFSW.QC.Command("-Unpause")]
        public static void UnPauseGame()
        {
            SetPause(false);
        }
        public static void TogglePause()
        {
            if (BlockTogglePause?.Invoke() == true)
                return;
            SetPause(!IsPaused);
        }
        [QFSW.QC.Command("-timescale")]
        public static void Command_SetTimescale(float timescale)
        {
            TimeSlowHandler.SetTimescale(timescale);
            UnPauseGame();
        }
        private void PressPauseInput(InputAction.CallbackContext c)
        {
            switch (c.phase)
            {
                case InputActionPhase.Disabled:
                    break;
                case InputActionPhase.Waiting:
                    break;
                case InputActionPhase.Started:
                    break;
                case InputActionPhase.Performed:
                    TogglePause();
                    break;
                case InputActionPhase.Canceled:
                    break;
                default:
                    break;
            }
        }
    }
    #endregion
    #region Application Determine
    public partial class GeneralManager
    {
        public static bool IsWebGL => Application.platform == RuntimePlatform.WebGLPlayer;
        public static bool IsEditor =>
#if UNITY_EDITOR
            true;
#else
            false;
#endif
    }
    #endregion
    [DefaultExecutionOrder(-10)]
    public partial class GeneralManager : MonoBehaviour
    {
        public static GeneralManager Instance { get; private set; }
        [SerializeField] InputActionReference pauseKeybind;
        private void Awake()
        {
            StartInstance();
        }
        [Initialize(-99959595)]
        private static void ClearInstance()
        {
            Instance = null;
        }
        [QFSW.QC.Command("FPS")]
        private static void SetFPS(int fps)
        {
            Application.targetFrameRate = fps.Clamp(5, 120);
        }
        private void OnDestroy()
        {
            if (Instance == this)
            {
                CloseInstance();
                if (pauseKeybind != null)
                {
                    pauseKeybind.action.performed -= PressPauseInput;
                    pauseKeybind.action.Disable();
                }
                SceneLoader.WhenStartLoadingAdditives -= PauseGame;
                SceneLoader.WhenFinishedLoadingAdditives -= UnPauseGame;
            }
        }
        private void Start()
        {
            if (Instance == this)
            {
                InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;
                TimeSlowHandler.Reload();
                SceneLoader.WhenStartLoadingAdditives += PauseGame;
                SceneLoader.WhenFinishedLoadingAdditives += UnPauseGame;
                if (pauseKeybind)
                {
                    pauseKeybind.action.Enable();
                    pauseKeybind.action.performed += PressPauseInput;
                }
            }
        }
        private void StartInstance()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            SetPause(false);
            DontDestroyOnLoad(gameObject);
        }
        private void CloseInstance()
        {
            if (Instance != this)
                return;
            Instance = null;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ReInitialize()
        {
            Instance = null;
        }
    }
}
