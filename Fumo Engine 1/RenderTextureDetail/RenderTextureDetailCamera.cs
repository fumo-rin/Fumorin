using UnityEngine;

namespace Fumorin
{
    [RequireComponent(typeof(Camera))]
    public class RenderTextureDetailCamera : MonoBehaviour
    {
        Camera cam;
        private void Awake()
        {
            cam = GetComponent<Camera>();
        }
        private void Start()
        {
            RenderTextureDetail.WhenSetTexture += SetTexture;
            if (RenderTextureDetail.GetCurrentTexture(out RenderTexture t))
            {
                SetTexture(t);            
            }
        }
        private void SetTexture(RenderTexture r)
        {
            cam.targetTexture = r;
        }
        private void OnDestroy()
        {
            RenderTextureDetail.WhenSetTexture -= SetTexture;
        }
    }
}
