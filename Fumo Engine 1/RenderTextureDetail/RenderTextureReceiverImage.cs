using UnityEngine;
using UnityEngine.UI;

namespace RinCore
{
    [RequireComponent(typeof(RawImage))]
    public class RenderTextureReceiverImage : MonoBehaviour
    {
        RawImage r;
        private void Awake()
        {
            r = GetComponent<RawImage>();
        }
        private void Start()
        {
            RenderTextureDetail.WhenSetTexture += SetTexture;
            if (RenderTextureDetail.GetCurrentTexture(out RenderTexture t))
            {
                SetTexture(t);
            }
            else
            {

            }
        }
        private void SetTexture(RenderTexture t)
        {
            r.texture = t;
        }
        private void OnDestroy()
        {
            RenderTextureDetail.WhenSetTexture -= SetTexture;
        }
    }
}
