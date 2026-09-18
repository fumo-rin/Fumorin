using System;
using UnityEngine;
using UnityEngine.UI;

namespace rinCore
{
    [RequireComponent(typeof(Button))]
    public abstract class GameSessionStarter : MonoBehaviour, IHierarchyComponentColor
    {
        #region Context menu for score key debugging
        [ContextMenu("UGS/Print Leaderboard Key")]
        private void ContextMenu_PrintLeaderboardKey()
        {
            if (session.Scoring is not null and SessionScoring s)
            {
                Debug.Log($"[GameSession2] UGS Leaderboard Key: '{session.LeaderboardKey}'\nLocal File Key: '{s.FileFriendlyKey}'");
            }
            else
            {
                Debug.LogWarning("[GameSession2] Cannot print score key - scoringData is null, >w<!");
            }
        }
        #endregion
        protected abstract GameSession2 session { get; }
        public Color LabelColor => ColorHelper.HierarchyTypes.SessionStarter;
        protected abstract void WhenPressed();
        protected void DefaultSessionLaunch(GameSession2 sess, Action payload)
        {
            GameSession2.BindSession(sess);
            new GameSession2.Events.ProgressScene(mode: GameSession2.Events.progressMode.NextOrMainMenu, payload, null, false).Publish();
        }
        void OnEnable()
        {
            GetComponent<Button>().BindSingleAction(() =>
            {
                WhenPressed();
            });
        }
    }
}
