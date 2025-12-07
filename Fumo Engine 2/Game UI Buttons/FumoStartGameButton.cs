using UnityEngine;
using UnityEngine.UI;
using FumoCore.Tools;

namespace Fumorin
{
    [RequireComponent(typeof(Button))]
    public abstract class FumoStartGameButton : MonoBehaviour
    {
        Button b;
        protected abstract string GamemodeString { get; }
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
            FumoLeaderboard.CurrentLeaderboardKey = (Application.productName + "_" + GamemodeString);
            Debug.Log("Starting Game...");
            StartGamePayload();
        }
        protected abstract void StartGamePayload();
    }
}
