using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace FumoCore.Tools
{
    [RequireComponent(typeof(UnityEngine.UI.Selectable))]
    public class PreventEventSystemUnselect : MonoBehaviour
    {
        private void LateUpdate()
        {
            if (!Helper.HasSelectWithEventSystem)
            {
                gameObject.Select_WithEventSystem();
            }
        }
    }
}