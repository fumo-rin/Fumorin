using UnityEngine;
using UnityEngine.UI;

namespace rinCore
{
    [RequireComponent(typeof(Button))]
    public class UnpauseButton : MonoBehaviour
    {
        Button b;
        private void Awake()
        {
            b = GetComponent<Button>();
        }
        private void Start()
        {
            b.AddClickAction(() => GeneralManager.SetPause(false));
        }
        private void OnDestroy()
        {
            b.RemoveAllClickActions();
        }
    }
}
