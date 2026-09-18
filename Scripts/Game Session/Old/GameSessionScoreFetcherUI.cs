using UnityEngine;
using TMPro;
namespace rinCore
{
    public class GameSessionScoreFetcherUI : MonoBehaviour
    {
        [SerializeField] GameSession.GameSessionScoreComponents scoreText;
        private void LateUpdate()
        {
            GameSession.ApplySessionToScoreComponents(scoreText);
        }
    }
}
