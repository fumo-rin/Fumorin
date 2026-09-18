using rinCore;
using UnityEngine;
using UnityEngine.UI;
namespace rinCore
{
    [RequireComponent(typeof(Button))]
    public class FumoMenuSceneButton : MonoBehaviour
    {
        Button b;
        [SerializeField] ScenePairSO sceneToLoad;
        [SerializeField] bool GameSession1_EndSession;
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
                        if (GameSession1_EndSession)
                        {
                            GameSession.EndSession(new()
                            {
                                SubmitScore = true
                            });
                        }
                    }
                });
        }
    }
}
