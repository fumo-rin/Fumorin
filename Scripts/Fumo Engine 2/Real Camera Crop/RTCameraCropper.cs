using UnityEngine;

namespace rinCore
{
    public class RTCameraCropper : MonoBehaviour
    {
        [SerializeField] Camera cropCamera;
        private void LateUpdate()
        {
            RectTransformCameraCropController.ApplyToCamera(cropCamera);
        }
    }
}
