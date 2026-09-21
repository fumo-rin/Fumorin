using rinCore;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
namespace rinCore
{
    [RequireComponent(typeof(Button))]
    public class FumoMenuSceneButton : MonoBehaviour
    {
        Button b;
        [SerializeField] ScenePairSO sceneToLoad;
        [System.Flags]
        public enum SessionEndingMode
        {
            None = FlagsRaw_Int._1,
            Uhhh = FlagsRaw_Int._2,
            GameSession1_EndSession = FlagsRaw_Int._3,
            GameSession2_EndAllSessions = FlagsRaw_Int._4,
        }
        [SerializeField] bool Deprecated_EndSession_GS1;
        [SerializeField] SessionEndingMode Mode = SessionEndingMode.None;
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
            if (sceneToLoad != null && !SceneLoader.IsLoading)
                SceneLoader.LoadScenePair(sceneToLoad, new()
                {
                    Delay = 0.05f,
                    FadeIn = 0.25f,
                    Payload = () =>
                    {
                        if (Deprecated_EndSession_GS1 || Mode.Match(SessionEndingMode.GameSession1_EndSession))
                        {
                            GameSession.EndSession(new()
                            {
                                SubmitScore = true
                            });
                        }
                        if (Mode.Match(SessionEndingMode.GameSession2_EndAllSessions))
                        {
                            GameSession2.ClearSessions(true);
                        }
                    }
                });
        }
    }
}
