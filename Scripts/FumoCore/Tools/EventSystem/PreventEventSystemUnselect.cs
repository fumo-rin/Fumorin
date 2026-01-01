using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace RinCore
{
    [RequireComponent(typeof(UnityEngine.UI.Selectable))]
    public class PreventEventSystemUnselect : MonoBehaviour
    {
        private void LateUpdate()
        {
            if (!RinHelper.HasSelectWithEventSystem)
            {
                gameObject.Select_WithEventSystem();
            }
        }
    }
}