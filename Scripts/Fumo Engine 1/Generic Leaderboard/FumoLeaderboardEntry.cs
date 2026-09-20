using TMPro;
using UnityEngine;
using rinCore;

namespace rinCore
{
    public class FumoLeaderboardEntry : MonoBehaviour
    {
        public const int NAME_CHAR_COUNT = 16;
        [SerializeField] TMP_Text rankText;
        [SerializeField] TMP_Text nameText;
        [SerializeField] TMP_Text scoreText;
        [SerializeField] TMP_Text seperatorText;

        public string CurrentPlayer => nameText != null ? nameText.text : string.Empty;
        public long CurrentScore => scoreText != null && long.TryParse(scoreText.text.Replace(" ", ""), out long result) ? result : 0;
        private string SeperatorString => "-";

        public void Clear()
        {
            Set(new LeaderboardCacheEntry(0, string.Empty, null));
        }

        public void Set(long score, string player, int rank = 0, LeaderboardMetadata metadata = null)
        {
            Set(new LeaderboardCacheEntry(score, player, metadata), rank);
        }

        public void Set(LeaderboardCacheEntry entry, int rank = 0)
        {
            if (seperatorText != null) seperatorText.text = "---";
            if (nameText != null) nameText.text = string.Empty;
            if (scoreText != null) scoreText.text = string.Empty;
            if (rankText != null) rankText.text = rank > 0 ? rank.ToString() : string.Empty;

            if (entry.score > 0)
            {
                if (nameText != null)
                {
                    nameText.text = entry.player.RemoveAfter("#").SafeString(true, true, false, true).ClampLength(16);
                }

                if (scoreText != null)
                    scoreText.text = entry.score.ToString().Numberize(" ");

                if (seperatorText != null)
                    seperatorText.text = SeperatorString;
            }
        }
    }
}