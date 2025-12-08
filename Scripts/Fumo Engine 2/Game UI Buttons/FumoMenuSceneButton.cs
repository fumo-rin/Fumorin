using RinCore;
using UnityEngine;
using UnityEngine.UI;
namespace RinCore
{
    [RequireComponent(typeof(Button))]
    public class FumoMenuSceneButton : MonoBehaviour
    {
        Button b;
        [SerializeField] ScenePairSO sceneToLoad;
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
            sceneToLoad.Load();
        }
    }
}
