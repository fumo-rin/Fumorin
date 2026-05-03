using rinCore.UGS;
using TMPro;
using UnityEngine;

namespace rinCore
{
    #region Scoring
    public partial class GameSession
    {
        #region Apply Text
        [System.Serializable]
        public struct GameSessionScoreComponents
        {
            public TMP_Text scoreText, highscoreText;
        }
        public static void TryApplySession(GameSessionScoreComponents components)
        {
            bool hasSession = CurrentAs(out GameSession session);
            if (components.scoreText is TMP_Text t1)
            {
                double score = 0f;
                if (hasSession)
                {
                    score = session.scoringData.ProcessedFinalScore;
                }
                t1.text = score.ToString();
            }
            if (components.highscoreText is TMP_Text t2)
            {
                double hiScore = 0f;
                if (hasSession)
                {
                    hiScore = session.scoringData.HighScore;
                }
                t2.text = hiScore.ToString();
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
            if (CurrentAs(out GameSession session))
            {
                var data = session.scoringData;
                PersistentJSON.SaveScore(data.ProcessedFinalScore, data.FileFriendlyKey);
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
            if (CurrentAs(out GameSession session))
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
        public class sessionData
        {
            private string cachedStorageKey = "default";
            public string ScoreStorageKey
            {
                get
                {
                    return cachedStorageKey;
                }
                set
                {
                    Debug.Log("Setting Storage key : " + value);
                    cachedStorageKey = value;
                    HighScore = TryFetchHighscore(cachedStorageKey);
                }
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
                    if (processedFinal > HighScore)
                    {
                        HighScore = processedFinal;
                    }
                    return processedFinal;
                }
            }
        }
        sessionData scoringData;
        public GameSession(sessionData data, bool cancelPrevious)
        {
            if (currentSession != null)
            {
                if (cancelPrevious)
                {
                    EndSession(new()
                    {
                        SubmitScore = false
                    });
                }
                else
                {
                    EndSession(new()
                    {
                        SubmitScore = true
                    });
                }
            }

            scoringData = data;
            StartSession(this);
        }
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
        private static void StartSession(GameSession s)
        {
            currentSession = s;
        }
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
        public static void EndSession(in EndSessionSettings settings)
        {
            if (settings.SubmitScore)
            {
                UploadLeaderboardSession();
            }
            currentSession = null;
        }

    }
}