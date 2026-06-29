using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace rinCore
{
    [RequireComponent(typeof(Slider))]
    public class SettingsSlider : MonoBehaviour, IHierarchyComponentColor
    {
        public Color LabelColor => ColorHelper.PastelGreen.Opacity(50);
        [SerializeField] TMP_Text sliderText;
        string storedSliderText;
        Slider slider;
        private void Awake()
        {
            slider = GetComponent<Slider>();
            storedSliderText = sliderText.text;
            slider.onValueChanged.RemoveAllListeners();
            slider.onValueChanged.AddListener((float v) =>
            {
                v *= 10f;
                GeneralManager.ApplyFramerate(v.ToInt(), 60, 120);
                sliderText.text = storedSliderText.RemoveAfter(":") + " " + v.ToString("F0");
            });
        }
        private void Start()
        {
            if (PersistentJSON.TryLoad(out int found, GeneralManager.FPS_SAVE_KEY))
            {
                float v = found.MultiplyAndFloorAsFloat(0.1f);
                slider.SetValues(v, 12f, 6f, true);
            }
        }
        private void OnDestroy()
        {
            slider.onValueChanged.RemoveAllListeners();
        }
    }
}
