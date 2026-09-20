using System;
using System.Collections.Generic;
using UnityEngine;
using rinCore.UGS;

namespace rinCore
{
    #region Scenes
    public partial class GameSession2
    {
        [System.Serializable]
        public class SessionScenes
        {
            public ScenePairSO ReturnScene, MainMenuScene;
            public List<ScenePairSO> orderedScenes = new();

            static SceneLoader.SceneLoadSettings MainMenuSettings => new()
            {
                Delay = 1.25f,
                FadeIn = 0.35f,
                FadeOut = 0f,
                Payload = () =>
                {
                    ClearSessions(true);
                }
            };

            public static void MissingSessionProgressEvent(Events.ProgressScene a)
            {
                SceneLoader.MainMenu(MainMenuSettings);
            }

            public void PerformNextSceneEvent(Events.ProgressScene a)
            {
                bool GoToScene(ScenePairSO s, SceneLoader.SceneLoadSettings? @override = null)
                {
                    if (s == null) return false;

                    SceneLoader.LoadScenePair(s, @override ?? new()
                    {
                        Delay = 0.05f,
                        FadeIn = 0.35f,
                        FadeOut = 0f,
                        ForceReload = false,
                    });
                    return true;
                }

                bool ConsumeNextScene(out ScenePairSO nextScene)
                {
                    nextScene = null;
                    if (orderedScenes != null && orderedScenes.Count > 0)
                    {
                        nextScene = orderedScenes[0];
                        orderedScenes.RemoveAt(0);
                        return true;
                    }
                    return false;
                }

                bool GoToNextOrFallback(ScenePairSO fallback, SceneLoader.SceneLoadSettings mainSettings, SceneLoader.SceneLoadSettings? fallbackSettings = null)
                {
                    if (ConsumeNextScene(out ScenePairSO next))
                    {
                        GoToScene(next, mainSettings);
                        return true;
                    }
                    if (fallback != null)
                    {
                        GoToScene(fallback, fallbackSettings);
                        return true;
                    }
                    return false;
                }

                SceneLoader.SceneLoadSettings standardSceneLoad = new()
                {
                    Delay = 0.05f,
                    FadeIn = 0.35f,
                    FadeOut = 0.35f,
                    Payload = a.Payload + (() =>
                    {
                        if (GameSession2.CurrentAs(out GameSession2 s))
                        {
                            s.Scoring.RefreshHighscore();
                        }
                    }),
                    PostUnloadPayload = a.PostUnloadPayload,
                    ForceReload = a.ForceReload
                };

                bool success = false;
                switch (a.mode)
                {
                    case Events.progressMode.NextOrMainMenu:
                        success = GoToNextOrFallback(MainMenuScene, standardSceneLoad, MainMenuSettings);
                        break;
                    case Events.progressMode.MainMenu:
                        success = GoToScene(MainMenuScene, MainMenuSettings);
                        break;
                    case Events.progressMode.NextOrReturnScene:
                        var exit = ReturnScene == null ? MainMenuScene : ReturnScene;
                        success = GoToNextOrFallback(exit, standardSceneLoad, standardSceneLoad);
                        break;
                    case Events.progressMode.NextOrNothing:
                        if (ConsumeNextScene(out ScenePairSO next))
                        {
                            GoToScene(next, standardSceneLoad);
                            success = true;
                        }
                        break;
                }

                if (!success)
                {
                    Debug.LogWarning("Non critical failure in next scene for session loop. Gracefully ending current session and forcing main menu.");
                    if (CurrentAs(out GameSession2 s))
                    {
                        EndSession(s);
                    }
                    ClearSessions(true);
                    SceneLoader.MainMenu();
                }
            }
        }
    }
    #endregion
    #region Actions
    public partial class GameSession2
    {
        public struct Events
        {
            public struct AddScore : IRinEvent
            {
                public double score;
                public bool quantized;
                public bool contributePopup;
            }
            public enum progressMode
            {
                NextOrMainMenu = 0,
                MainMenu = -100,
                NextOrReturnScene = 200,
                NextOrNothing = 300,
            }
            public record ProgressScene(progressMode mode, Action Payload, Action PostUnloadPayload, bool ForceReload) : IRinEvent;
        }

        [Initialize(0)]
        public static void ClearSessions(bool submitValidScores)
        {
            if (sessionStack != null)
            {
                foreach (var item in sessionStack)
                {
                    if (item == null) continue;
                    if (item.Scoring != null && submitValidScores)
                    {
                        item.Scoring.StoreAndUploadScore();
                    }
                }
            }
            sessionStack = new();
            ClearSessionEvents();
        }

        private static void ClearSessionEvents()
        {
            EventBus.Clear<Events.AddScore>();
            EventBus.Clear<Events.ProgressScene>();
            EventBus.Bind<Events.ProgressScene>(SessionScenes.MissingSessionProgressEvent);
        }

        protected virtual void OnAddScore(Events.AddScore a)
        {
            if (a.quantized)
            {
                Scoring.RawScore += a.score;
            }
            else
            {
                Scoring.RawExtrasScore += a.score;
            }
        }

        protected virtual void WhenBind()
        {
            EventBus.Release<Events.ProgressScene>(SessionScenes.MissingSessionProgressEvent);
            EventBus.Bind<Events.ProgressScene>(scenes.PerformNextSceneEvent);
            EventBus.Bind<Events.AddScore>(OnAddScore);
        }

        protected virtual void WhenUnbind()
        {
            EventBus.Release<Events.ProgressScene>(scenes.PerformNextSceneEvent);
            EventBus.Release<Events.AddScore>(OnAddScore);
        }

        protected virtual void WhenEnd() { }
    }
    #endregion
    [System.Serializable]
    public partial class GameSession2
    {
        [SerializeField] protected SessionScenes scenes;
        [SerializeField] protected SessionScoring scoringData;
        public SessionScoring Scoring => scoringData;
        public string LeaderboardKey => scoringData.ScoreStorageKey;

        static Stack<GameSession2> sessionStack = new();

        public static bool CurrentAs<T>(out T sess) where T : GameSession2
        {
            sess = null;
            if (sessionStack == null || sessionStack.Count == 0)
            {
                return false;
            }

            if (sessionStack.Peek() is T typedSess)
            {
                sess = typedSess;
                return true;
            }

            return false;
        }

        public static void BindSession(GameSession2 sess)
        {
            if (sessionStack == null)
            {
                sessionStack = new();
            }

            if (sessionStack.TryPeek(out var activeSession))
            {
                activeSession.WhenUnbind();
            }

            sessionStack.Push(sess);
            ClearSessionEvents();
            sess.WhenBind();
        }

        [NYI("Score Validation")]
        public static void EndSession(GameSession2 sess)
        {
            if (sessionStack == null || sessionStack.Count == 0)
            {
                return;
            }

            if (sessionStack.Peek() == sess)
            {
                sessionStack.Pop();
                sess.Scoring.StoreAndUploadScore();
                sess.WhenUnbind();
                sess.WhenEnd();

                ClearSessionEvents();

                if (sessionStack.TryPeek(out var previousSession))
                {
                    previousSession.WhenBind();
                }
            }
        }
    }
}