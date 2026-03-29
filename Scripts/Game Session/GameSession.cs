using UnityEngine;

namespace rinCore
{
    public abstract class GameSession
    {
        [System.Serializable]
        public class sessionData
        {
            public string UserName;
            public double SessionScore;
            public double SessionHighScore;
        }
        sessionData currentSessionData;
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

            currentSessionData = data;
            currentSession = this;
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

            }
            currentSession = null;
        }
    }
}