using RinCore;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem.Composites;
using UnityEngine.UI;

namespace RinCore
{
    [RequireComponent(typeof(Button))]
    public class RenderTextureDetailButton : MonoBehaviour
    {
        [SerializeField] RenderTextureDetail.RenderMode detail = RenderTextureDetail.RenderMode.Highres;
        private void Start()
        {
            Button button = GetComponent<Button>();
            switch (detail)
            {
                case RenderTextureDetail.RenderMode.VeryHighres:
                    button.gameObject.GetComponentInChildren<TMP_Text>().text = "960x1280";
                    button.AddClickAction(VeryHighRes);
                    break;
                case RenderTextureDetail.RenderMode.Highres:
                    button.gameObject.GetComponentInChildren<TMP_Text>().text = "600x800";
                    button.AddClickAction(Highres);
                    break;
                case RenderTextureDetail.RenderMode.Lowres:
                    button.gameObject.GetComponentInChildren<TMP_Text>().text = "360x480";
                    button.AddClickAction(Lowres);
                    break;
                default:
                    break;
            }
        }
        private void OnDestroy()
        {
            Button button = GetComponent<Button>();
            switch (detail)
            {
                case RenderTextureDetail.RenderMode.VeryHighres:
                    button.RemoveClickAction(VeryHighRes);
                    break;
                case RenderTextureDetail.RenderMode.Highres:
                    button.RemoveClickAction(Highres);
                    break;
                case RenderTextureDetail.RenderMode.Lowres:
                    button.RemoveClickAction(Lowres);
                    break;
                default:
                    break;
            }
        }
        private void VeryHighRes()
        {
            RenderTextureDetail.SetMode(RenderTextureDetail.RenderMode.VeryHighres);
        }
        private void Highres()
        {
            RenderTextureDetail.SetMode(RenderTextureDetail.RenderMode.Highres);
        }
        private void Lowres()
        {
            RenderTextureDetail.SetMode(RenderTextureDetail.RenderMode.Lowres);
        }
    }
}
