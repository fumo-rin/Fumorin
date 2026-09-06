using UnityEngine;

namespace rinCore
{
    public class RectTransformCameraCropController : MonoBehaviour
    {
        static RectTransformCameraCropController instance;
        [SerializeField] Camera uiCamera;
        [SerializeField] RectTransform viewPortRect;
        static Rect storedRect;
        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                return;
            }
            Destroy(gameObject);
        }
        private void LateUpdate()
        {
            if (instance)
            {
                Vector3[] corners = new Vector3[4];
                instance.viewPortRect.GetWorldCorners(corners);

                Vector2 bottomLeft = RectTransformUtility.WorldToScreenPoint(
                    instance.uiCamera,
                    corners[0]);

                Vector2 topRight = RectTransformUtility.WorldToScreenPoint(
                    instance.uiCamera,
                    corners[2]);

                float left = Mathf.Round(bottomLeft.x);
                float bottom = Mathf.Round(bottomLeft.y);
                float right = Mathf.Round(topRight.x);
                float top = Mathf.Round(topRight.y);

                Rect pixelRect = new Rect(
                    left,
                    bottom,
                    right - left,
                    top - bottom
                );
                storedRect = pixelRect;
            }
        }
        [Initialize(-9999)]
        static void ReinitializeStatic()
        {
            instance = null;
        }
        public static void ApplyToCamera(Camera scaleCamera, bool clearColor)
        {
            scaleCamera.clearFlags = CameraClearFlags.Depth;
            if (clearColor)
                scaleCamera.backgroundColor = Color.black.Opacity(0);
            scaleCamera.pixelRect = storedRect;
        }
    }
}
