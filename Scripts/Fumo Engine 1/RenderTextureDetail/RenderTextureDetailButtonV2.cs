using RinCore;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace rinCore
{
    [RequireComponent(typeof(Button))]
    public class RenderTextureDetailButtonV2 : MonoBehaviour
    {
        Button b;
        [SerializeField] TMPro.TMP_Text itemText;
        [SerializeField] int width = 360, height = 480;
        private void Awake()
        {
            b = GetComponent<Button>();
            if (itemText != null)
            {
                itemText.text = $"{width.ToString()}x{height.ToString()}";
            }
        }
        private void Start()
        {
            b.AddClickAction(SendToHandler);
        }
        private void OnDestroy()
        {
            b.RemoveClickAction(SendToHandler);
        }
        private void SendToHandler()
        {
            RenderTextureIndividualPart.SetNewSize(width, height);
        }
    }
}
