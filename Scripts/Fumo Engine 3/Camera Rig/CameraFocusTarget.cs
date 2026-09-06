using UnityEngine;
using Unity.Cinemachine;

namespace rinCore
{
    [DefaultExecutionOrder(-99)]
    public class CameraFocusTarget : MonoBehaviour
    {
        [SerializeField] private CinemachineCamera _camera;
        private void OnEnable()
        {
            EventBus.Bind<FEB_Camera_Focus>(SetNewTarget);
        }

        private void OnDisable()
        {
            EventBus.Release<FEB_Camera_Focus>(SetNewTarget);
        }

        private void SetNewTarget(FEB_Camera_Focus action)
        {
            if (_camera == null) return;

            if (action.focus == null)
            {
                if (_camera.Target.TrackingTarget != null)
                {
                    IFocusCamera.FallbackFocusObject.transform.position = _camera.Target.TrackingTarget.position;
                }
                _camera.Target.TrackingTarget = IFocusCamera.FallbackFocusObject.transform;
            }
            else
            {
                _camera.Target.TrackingTarget = action.focus.transform;
            }
        }
    }
    public record FEB_Camera_Focus(GameObject focus);
    public record FEB_Camera_Offset(float x, float y);
    public interface IFocusCamera
    {
        private static GameObject _fallbackFocusObject;
        public static GameObject FallbackFocusObject
        {
            get
            {
                if (_fallbackFocusObject == null)
                {
                    _fallbackFocusObject = new GameObject("Temporary_Camera_Focus");
                    Object.DontDestroyOnLoad(_fallbackFocusObject);
                }
                return _fallbackFocusObject;
            }
        }

        public void SetFocus(GameObject g, CinemachineCamera cam)
        {
            if (cam == null) return;
            if (g == null)
            {
                if (cam.Target.TrackingTarget != null)
                {
                    FallbackFocusObject.transform.position = cam.Target.TrackingTarget.position;
                }

                cam.Target.TrackingTarget = FallbackFocusObject.transform;
            }
            else
            {
                cam.Target.TrackingTarget = g.transform;
            }
        }

        public void ClearFocus(CinemachineCamera cam) => SetFocus(null, cam);
    }
}