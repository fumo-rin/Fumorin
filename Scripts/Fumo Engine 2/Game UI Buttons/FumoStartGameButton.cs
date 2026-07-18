using UnityEngine;
using UnityEngine.UI;
using rinCore;

namespace rinCore
{
    [RequireComponent(typeof(Button))]
    public abstract class FumoStartGameButton : MonoBehaviour
    {
        protected Button button;
        protected abstract string LeaderboardKey { get; }
        private void Awake()
        {
            button = GetComponent<Button>();
        }
        private void Start()
        {
            button.BindSingleEventAction(PressStart);
        }
        protected virtual void WhenAwake()
        {

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
