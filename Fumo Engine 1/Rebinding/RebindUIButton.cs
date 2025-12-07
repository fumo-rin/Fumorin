using UnityEngine;
using UnityEngine.UI;

namespace Fumorin
{
    [RequireComponent(typeof(Button))]
    public class RebindUIButton : MonoBehaviour
    {
        public enum KeyFunction
        {
            ToggleUI,
            ResetBinds,
        }
        Button b;
        [SerializeField] KeyFunction pressEnum = KeyFunction.ToggleUI;
        private void Awake()
        {
            b = GetComponent<Button>();
        }
        void Start()
        {
            b.onClick.AddListener(PressButton);
        }
        private void OnDestroy()
        {
            b.onClick.RemoveListener(PressButton);
        }
        private void PressButton()
        {
            switch (pressEnum)
            {
                case KeyFunction.ToggleUI:
                    bool shouldShow = !RebindHandler.IsVisible;
                    RebindHandler.SetUIVisibility(shouldShow);
                    break;
                case KeyFunction.ResetBinds:
                    RebindHandler.ResetToDefaults();
                    break;
                default:
                    break;
            }
        }
    }
}
