using RinCore;
using RinCore;
using System.Collections;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using System;
using UnityEditor;

namespace RinCore
{
    #region Funny Explosion
    public partial class GeneralManager
    {
        [SerializeField] GameObject funnyExplosion;
        [SerializeField] ACWrapper funnyExplosionSound;
        public static void FunnyExplosion(Vector2 position, float scale = 1f)
        {
            GameObject x = Instantiate(Instance.funnyExplosion, position, Quaternion.identity);
            Destroy(x, 1.02f);
            x.transform.localScale *= scale;
            x.transform.localScale = new(x.transform.localScale.x.Max(0.25f), x.transform.localScale.y.Max(0.25f), 1);
            Instance.funnyExplosionSound.Play(position);
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
    #region Score
    public partial class GeneralManager
    {
        private static double HiddenScoreValidationSum = 0;
        private static float ScoreValidationMultiplier;
        public static bool ShouldAddScoreKey => ScoreBreakdownAnalysis != null;
        public static void AddScoreAnalysisKey(string scoreKey, double score)
        {
            if (!ShouldAddScoreKey)
                return;
            if (!ScoreBreakdownAnalysis.ContainsKey(scoreKey))
                ScoreBreakdownAnalysis[scoreKey] = 0;
            ScoreBreakdownAnalysis[scoreKey] += score;
        }
        public delegate bool ScoreValidation();
        public static event ScoreValidation WhenValidateScore;
        public static bool IsScoreLegit()
        {
            bool InternalCheck()
            {
                double scoreAccuracy = HiddenScoreValidationSum / ScoreValidationMultiplier;
                Debug.Log($"Score Accuracy : {scoreAccuracy} - {actualScore} = " + (scoreAccuracy - actualScore));
                if (scoreAccuracy - actualScore < (actualScore * 0.05d))
                {
                    Debug.Log("Score Seems Good");
                    return true;
                }
                if (actualScore > 0f)
                {
                    Debug.LogWarning("Score Seems Not Good");
                }
                return false;
            }
            bool ExternalCheck()
            {
                if (WhenValidateScore != null)
                {
                    foreach (ScoreValidation validator in WhenValidateScore.GetInvocationList())
                    {
                        if (!validator.Invoke())
                        {
                            Debug.LogWarning("Score invalidated by external component : " + validator.GetType().Name);
                            return false;
                        }
                    }
                }
                return true;
            }
            return InternalCheck() && ExternalCheck();
        }
        public static double SumUpScoreAnalysis(bool debugPrint)
        {
            double sum = 0f;
            if (ScoreBreakdownAnalysis == null)
            {
                return sum;
            }
            foreach (var item in ScoreBreakdownAnalysis)
            {
                if (debugPrint)
                {
                    Debug.Log($"Score Breakdown({item.Key}) : {item.Value.ToString("F0")}");
                }
                sum += item.Value;
            }
            return sum;
        }
        private static Dictionary<string, double> ScoreBreakdownAnalysis;
        [SerializeField] bool breakDownScore = false;
        public static double actualScore;
        public static double addedExtraScore;
        public static double HighestScore { get; private set; }
        public static double VisibleScore => PreProcessedScore + addedExtraScore;
        public static double PreProcessedScore { get; private set; }
        [SerializeField] double visibleScoreDivisor = 0.01d;
        [SerializeField] double visibleScoreMultiplier = 100d;
        static string HighScoreChangeableKey = "";
        static string HighScoreStringKey => BuildHighscoreStringKey(HighScoreChangeableKey);
        static string BuildHighscoreStringKey(string extra)
        {
            return "HiScore Save" + extra;
        }
        static bool IsHighscorePotentiallyOutOfSync = true;
        public delegate void ScoreAction(double score, double hiscore);
        public static ScoreAction OnScoreUpdate;
        public static void ChangeScoreKey(string newKey)
        {
            HighScoreChangeableKey = newKey;
            IsHighscorePotentiallyOutOfSync = true;
            LoadHighScore();
        }
        public static void RequestScoreRefresh()
        {
            Debug.Log("T");
            IsHighscorePotentiallyOutOfSync = true;
            LoadHighScore();
            AddScore(0d, false);
        }
        public static double LoadHighScore()
        {
            ResyncHighscore();
            return HighestScore;
        }
        public static void StoreAndResetScore()
        {
            long submittedScore = VisibleScore.ToLong();
            bool isLegit = IsScoreLegit();
            SetScoreValue(0f, false);
            addedExtraScore = 0f;
            ScoreBreakdownAnalysis.Clear();
            HiddenScoreValidationSum = 0;
            if (isLegit && !GeneralManager.IsEditor)
            {
                _ = FumoLeaderboard.SubmitScoreAsync(submittedScore);
            }
        }
        [QFSW.QC.Command("-reset-score")]
        private void ResetHighscore(string difficulty)
        {
            PlayerPrefs.DeleteKey(HighScoreStringKey);
            IsHighscorePotentiallyOutOfSync = true;
            ResyncHighscore();
        }
        private static void SendUpdateScoreEvent(double scoreValue, double highScoreValue)
        {
            OnScoreUpdate?.Invoke(scoreValue, highScoreValue);
        }
        public static double AddScore(double value, bool withoutHighscore)
        {
            SetScoreValue(actualScore + value, withoutHighscore);
            HiddenScoreValidationSum += value * ScoreValidationMultiplier;
            return value;
        }
        public static double AddRawExtraScore(double value, bool withoutHighscore)
        {
            addedExtraScore += value;
            HiddenScoreValidationSum += value * ScoreValidationMultiplier;
            SetScoreValue(actualScore, withoutHighscore);
            return value;
        }
        public static void ApplyHighscoreToSave(double value)
        {
            value.StoreKey(HighScoreStringKey);
            Debug.Log($"Storing Score ({HighScoreStringKey}) : " + value.ToString("F0"));
        }
        static void ResyncHighscore()
        {
            if (!IsHighscorePotentiallyOutOfSync)
                return;

            IsHighscorePotentiallyOutOfSync = false;

            double loadedScore = 0d.FetchKey(HighScoreStringKey);
            Debug.Log($"Loading Score ({HighScoreStringKey}) : " + loadedScore.ToString("F0"));

            HighestScore = loadedScore;
        }
        private static void SetScoreValue(double value, bool withoutHighscore)
        {
            ResyncHighscore();
            actualScore = value;
            PreProcessedScore = (value.Multiply(Instance.visibleScoreDivisor)).Floor().Multiply(Instance.visibleScoreMultiplier);

            if (!withoutHighscore && actualScore > HighestScore)
            {
                HighestScore = VisibleScore;
            }
            SendUpdateScoreEvent(VisibleScore, HighestScore);
        }
        private void OnApplicationQuit()
        {
            if (!GeneralManager.IsEditor)
            {
                StoreScore(VisibleScore.ToLong());
            }
        }
        public static void StoreScore(long score)
        {
            if (score <= 0)
            {
                return;
            }
            ApplyHighscoreToSave(LoadHighScore());
            if (ScoreBreakdownAnalysis != null)
            {
                foreach (var item in ScoreBreakdownAnalysis)
                {
                    string scoreMessage = "Score Breakdown##".ReplaceLineBreaks("##");
                    scoreMessage += $"Score Partition({item.Key}) : {item.Value.ToString("F0")}##".ReplaceLineBreaks("##");
                }
            }
            if (IsScoreLegit())
            {
                long LeaderboardScore = score;
                _ = FumoLeaderboard.SubmitScoreAsync(LeaderboardScore);
            }
        }
    }
    #endregion
    #region Score Key Debugger
#if UNITY_EDITOR
    [InitializeOnLoad]
    public static class PlaymodeScoreDebugger
    {
        static PlaymodeScoreDebugger()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                GeneralManager.SumUpScoreAnalysis(true);
            }
        }
    }
#endif
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
                IsHighscorePotentiallyOutOfSync = true;
                SetScoreValue(0f, false);
                ScoreValidationMultiplier = UnityEngine.Random.Range(1f, 10f);
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
            if (breakDownScore)
            {
                ScoreBreakdownAnalysis = new();
            }
            StoreAndResetScore();
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
            ScoreBreakdownAnalysis = null;
        }
    }
}
