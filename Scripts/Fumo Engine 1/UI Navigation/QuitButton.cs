using rinCore;
using UnityEngine;
using UnityEngine.UI;

namespace rinCore
{
    [RequireComponent(typeof(Button))]
    public class QuitButton : MonoBehaviour
    {
        Button b;
        private void Awake()
        {
            b = GetComponent<Button>();
        }
        private void Start()
        {
            b.AddClickAction(() => Application.Quit());
            b.interactable = !GeneralManager.IsWebGL && !GeneralManager.IsEditor;
            b.GetComponent<Image>().raycastTarget = b.interactable;
        }
        private void OnDestroy()
        {
            b.RemoveClickAction(() => Application.Quit());
        }
    }
}
