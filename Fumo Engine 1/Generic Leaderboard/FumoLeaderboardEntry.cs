using TMPro;
using UnityEngine;
using FumoCore.Tools;

namespace Fumorin
{
    public class FumoLeaderboardEntry : MonoBehaviour
    {
        [SerializeField] TMP_Text nameText;
        [SerializeField] TMP_Text scoreText;
        [SerializeField] TMP_Text seperatorText;
        public string CurrentPlayer => nameText.text;
        public long CurrentScore => long.TryParse(scoreText.text, out long result) ? result : 0;
        string seperatorString => "-";
        public void Clear()
        {
            Set(0, "", 0);
        }
        public void Set(long score, string player, int rank)
        {
            seperatorText.text = "---";
            nameText.text = "";
            scoreText.text = "";
            if (score > 0)
            {
                nameText.text = player.RemoveAfter("#").SafeString(true, true, false, true);
                scoreText.text = score.ToString().Numberize(" ");
                seperatorText.text = seperatorString;
            }
        }
    }
}