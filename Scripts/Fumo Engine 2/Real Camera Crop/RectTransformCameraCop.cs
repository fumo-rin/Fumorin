using UnityEngine;

namespace rinCore
{
    public class RectTransformCameraCop : MonoBehaviour
    {
        [SerializeField] Camera cropCamera, uiCamera;
        [SerializeField] RectTransform UICrop;
        private void LateUpdate()
        {
            cropCamera.pixelRect = RectTransformToPixelRect(UICrop, uiCamera);
        }
        public static Rect RectTransformToPixelRect(RectTransform rt, Camera uiCamera)
        {
            Vector3[] corners = new Vector3[4];
            rt.GetWorldCorners(corners);

            Vector2 bottomLeft = RectTransformUtility.WorldToScreenPoint(
                uiCamera,
                corners[0]);

            Vector2 topRight = RectTransformUtility.WorldToScreenPoint(
                uiCamera,
                corners[2]);

            float left = Mathf.Round(bottomLeft.x);
            float bottom = Mathf.Round(bottomLeft.y);
            float right = Mathf.Round(topRight.x);
            float top = Mathf.Round(topRight.y);

            return new Rect(
                left,
                bottom,
                right - left,
                top - bottom
            );
        }
    }
}
