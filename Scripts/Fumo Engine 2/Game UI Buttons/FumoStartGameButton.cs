using UnityEngine;
using UnityEngine.UI;
using rinCore;

namespace rinCore
{
    [RequireComponent(typeof(Button))]
    public abstract class FumoStartGameButton : MonoBehaviour
    {
        Button b;
        protected abstract string LeaderboardKey { get; }
        private void Awake()
        {
            b = GetComponent<Button>();
        }
        private void Start()
        {
            b.BindSingleEventAction(PressStart);
        }
        private void PressStart()
        {
            FumoLeaderboard.CurrentLeaderboardKey = LeaderboardKey;
            Debug.Log("Starting Game with Leaderboard Key : " + LeaderboardKey);
            StartGamePayload();
        }
        protected abstract void StartGamePayload();
    }
}
