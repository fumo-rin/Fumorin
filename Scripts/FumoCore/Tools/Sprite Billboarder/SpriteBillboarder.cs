using UnityEngine;

namespace rinCore
{
    public class SpriteBillboarder : MonoBehaviour
    {
        static Camera cachedCamera;
        Camera Cam
        {
            get
            {
                return cachedCamera ?? Camera.main;
            }
        }
        private void LateUpdate()
        {
            if (Cam)
            {
                transform.LookAt(Cam.transform.position);
                transform.localRotation = Quaternion.Euler(0f, transform.localRotation.eulerAngles.y + 180, 0f);
            }
        }
    }
}
