using UnityEngine;
using UnityEngine.InputSystem;

namespace rinCore
{
    public class ClickNavigator : MonoBehaviour
    {
        [SerializeField] private RunnableObjectNavigator objectNavigator;
        [SerializeField] private Camera overrideCam;
        [SerializeField] private LayerMask clickMask;

        private Camera Cam => overrideCam != null ? overrideCam : Camera.main;

        private void LateUpdate()
        {
            HandleClick();
        }

        private void HandleClick()
        {
            if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
                return;

            Camera cam = Cam;
            if (cam == null)
                return;

            Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (!Physics.Raycast(ray, out RaycastHit hit, 500f, clickMask, QueryTriggerInteraction.Ignore))
                return;

            GeneralManager.FunnyExplosion(new()
            {
                is3d = true,
                playSound = true,
                position = hit.point,
                scale = 2f
            });

            if (objectNavigator != null)
            {
                objectNavigator.SetNewTarget(hit.point);
            }
        }
    }
}