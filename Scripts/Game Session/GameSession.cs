using rinCore.UGS;
using TMPro;
using UnityEngine;

namespace rinCore
{
    #region Stalled Game Logic
    public abstract partial class GameSession
    {
        public virtual bool GameLogicStalled { get; }
    }
    #endregion
    #region Scoring
    public partial class GameSession
    {
        #region Invalidation
        public delegate bool ScoreSessionInvalidator();
        public static event ScoreSessionInvalidator WhenInvalidationCheck;
        private static bool IsSessionScoreValid() => WhenInvalidationCheck?.Invoke() ?? true;
        #endregion

        #region Apply Text
        [System.Serializable]
        public struct GameSessionScoreComponents
        {
            public TMP_Text scoreText, highscoreText;
        }
        public static void ApplySessionToScoreComponents(GameSessionScoreComponents components)
        {
            bool hasSession = CurrentAs(out GameSession session);
            if (components.scoreText is TMP_Text t1)
            {
                double score = 0f;
                if (hasSession)
                {
                    score = session.scoringData.ProcessedFinalScore;
                }
                t1.text = score.ToThousandsString(0, " ");
            }
            if (components.highscoreText is TMP_Text t2)
            {
                double hiScore = 0f;
                if (hasSession)
                {
                    hiScore = session.scoringData.HighScore;
                }
                t2.text = hiScore.ToThousandsString(0, " ");
            }
        }
        #endregion
        [QFSW.QC.Command("-sTest-ugs-name")]
        public static void SetPlayer(string name)
        {
            _ = UGSInitializer.SetPlayerNameAsync(name);
        }
        [QFSW.QC.Command("-sTest-addscore-test")]
        public static bool TryAddScoreRaw(double rawScore, string scoreItemName)
        {
            if (CurrentAs(out GameSession session))
            {
                var data = session.scoringData;
                data.RawScore += rawScore;
                double testDebug = data.ProcessedFinalScore;
                return true;
            }
            return false;
        }
        public static double ReadCurrentRawScore(out double score)
        {
            score = 0d;
            if (CurrentAs(out GameSession session))
            {
                var data = session.scoringData;
                score = data.RawScore;
            }
            return score;
        }
        [QFSW.QC.Command("-sTest-addscore-extras")]
        public static bool TryAddScoreExtras(double rawExtrasScore, string scoreItemName)
        {
            if (CurrentAs(out GameSession session))
            {
                var data = session.scoringData;
                data.RawExtrasScore += rawExtrasScore;


                double testDebug = data.ProcessedFinalScore;


                return true;
            }
            return false;
        }
        public static bool TryStoreSessionHighscore()
        {
            if (CurrentAs(out GameSession session) && IsSessionScoreValid())
            {
                var data = session.scoringData;
                PersistentJSON.LoadScore(data.FileFriendlyKey, out double storedScore);
                if (data.ProcessedFinalScore > storedScore)
                {
                    PersistentJSON.SaveScore(data.ProcessedFinalScore, data.FileFriendlyKey);
                }
                return true;
            }
            return false;
        }
        public static double TryFetchHighscore(string key)
        {
            double score = 0d;
            string fileFriendlyKey = Application.productName.SafeRemoveWords() + "_" + key;
            PersistentJSON.LoadScore(fileFriendlyKey, out score);
            return score;
        }
        [QFSW.QC.Command("-sTest-ugs-storescore")]
        public static void UploadLeaderboardSession()
        {
            if (CurrentAs(out GameSession session) && IsSessionScoreValid())
            {
                var data = session.scoringData;
                long submitableScore = session.scoringData.ProcessedFinalScore.ToLong();

                bool isLegit = true;
                if (isLegit/* && !GeneralManager.IsEditor*/)
                {
                    FumoLeaderboard.CurrentLeaderboardKey = session.scoringData.ScoreStorageKey;
                    _ = FumoLeaderboard.SubmitScoreAsync(submitableScore);
                }
            }
        }
    }
    #endregion
    public abstract partial class GameSession
    {
        [System.Serializable]
        public class scoringSession
        {
            [field: SerializeField] public string ScoreStorageKey { get; private set; } = "default";
            public void Reset()
            {
                RawScore = 0d;
                RawExtrasScore = 0d;
            }
            public void Continue()
            {
                RawScore = 0d;
                RawExtrasScore += 1d;
            }
            public string FileFriendlyKey => Application.productName.SafeRemoveWords() + "_" + ScoreStorageKey.SafeRemoveWords();
            public double RawScore;
            public double RawExtrasScore;
            public double HighScore;
            public double ScoreDivisor = 100d;
            public double ProcessedFinalScore
            {
                get
                {
                    double processedVisibleScore = ((float)RawScore).ReverseQuantize(((float)ScoreDivisor));
                    double processedFinal = processedVisibleScore + RawExtrasScore;
                    if (processedFinal > HighScore && IsSessionScoreValid())
                    {
                        HighScore = processedFinal;
                    }
                    return processedFinal;
                }
            }
        }
        [SerializeField] protected scoringSession scoringData;
        public string LeaderboardKey => scoringData.ScoreStorageKey;
        public bool SessionAs<T>(out T result) where T : GameSession
        {
            if (this is T t)
            {
                result = t;
                return true;
            }
            result = null;
            return false;
        }
        static GameSession currentSession;
        public static void StartSession(GameSession s)
        {
            if (currentSession != null)
            {
                EndSession(new()
                {
                    SubmitScore = false
                });
            }
            s.scoringData.Reset();
            currentSession = s;
            s.WhenStartSession();
            s.scoringData.HighScore = TryFetchHighscore(s.scoringData.ScoreStorageKey);
        }
        protected abstract void WhenStartSession();
        protected abstract void WhenEndSession();
        public static bool CurrentAs<T>(out T result) where T : GameSession
        {
            result = null;
            if (currentSession == null)
            {
                return false;
            }
            return currentSession.SessionAs(out result);
        }
        public class EndSessionSettings
        {
            public bool SubmitScore = false;
        }
        public static void EndSession(EndSessionSettings settings)
        {
            if (TryStoreSessionHighscore())
            {
                Debug.Log("Stored Highscore");
            }
            else
            {
                Debug.Log("Didn't Store Highscore.");
            }
            if (settings.SubmitScore)
            {
                UploadLeaderboardSession();
            }
            if (CurrentAs(out GameSession s))
            {
                s.WhenEndSession();
            }
            currentSession = null;
        }
    }
}