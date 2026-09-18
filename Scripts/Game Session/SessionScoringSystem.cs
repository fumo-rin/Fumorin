using TMPro;
using UnityEngine;
using rinCore.UGS;

namespace rinCore
{
    [System.Serializable]
    public class SessionScoring
    {
        [field: SerializeField] public string ScoreStorageKey { get; private set; } = "default";

        public double RawScore;
        public double RawExtrasScore;
        public double HighScore;
        public double ScoreDivisor = 100d;

        public string FileFriendlyKey => Application.productName.SafeRemoveWords() + "_" + ScoreStorageKey.SafeRemoveWords();

        public double ProcessedFinalScore
        {
            get
            {
                double processedVisibleScore = ((float)RawScore).ReverseQuantize((float)ScoreDivisor);
                double processedFinal = processedVisibleScore + RawExtrasScore;
                if (processedFinal > HighScore)
                {
                    HighScore = processedFinal;
                }
                return processedFinal;
            }
        }

        public void Reset()
        {
            RawScore = 0d;
            RawExtrasScore = 0d;
        }

        public void RefreshHighscore()
        {
            HighScore = FetchHighscore(ScoreStorageKey);
        }

        public void MergeFrom(SessionScoring childScoring, bool includeExtras = true)
        {
            RawScore += childScoring.ProcessedFinalScore;
            if (includeExtras)
            {
                RawExtrasScore += childScoring.RawExtrasScore;
            }
        }

        public void StoreAndUploadScore()
        {
            PersistentJSON.LoadScore(FileFriendlyKey, out double storedScore);
            if (ProcessedFinalScore > storedScore)
            {
                PersistentJSON.SaveScore(ProcessedFinalScore, FileFriendlyKey);
            }

            long submitableScore = ProcessedFinalScore.ToLong();
            FumoLeaderboard.CurrentLeaderboardKey = ScoreStorageKey;
            _ = FumoLeaderboard.SubmitScoreAsync(submitableScore);
        }

        public static double FetchHighscore(string key)
        {
            string fileFriendlyKey = Application.productName.SafeRemoveWords() + "_" + key;
            PersistentJSON.LoadScore(fileFriendlyKey, out double score);
            return score;
        }

        [System.Serializable]
        public struct ScoreComponents
        {
            public TMP_Text scoreText, highscoreText;
        }

        public void ApplyToScoreComponents(ScoreComponents components)
        {
            if (components.scoreText is TMP_Text t1)
            {
                t1.text = ProcessedFinalScore.ToThousandsString(0, " ");
            }
            if (components.highscoreText is TMP_Text t2)
            {
                t2.text = HighScore.ToThousandsString(0, " ");
            }
        }
    }
}