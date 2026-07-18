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
        [SerializeField] bool EndSession;
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
            if (sceneToLoad != null)
                SceneLoader.LoadScenePair(sceneToLoad, new()
                {
                    Delay = 0.1f,
                    Payload = () =>
                    {
                        if (EndSession)
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
