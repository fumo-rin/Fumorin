using RinCore;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace rinCore
{
    [RequireComponent(typeof(Slider))]
    public class DeadzoneSlider : MonoBehaviour
    {
        Slider s;
        [SerializeField] TMP_Text optionalText;
        string storedString;
        [SerializeField] float scalingFactor = 1000f;
        private void Awake()
        {
            s = GetComponent<Slider>();
            storedString = "";
            if (optionalText)
            {
                storedString += optionalText.text;
            }
        }
        private void Start()
        {
            s.onValueChanged.AddListener(SliderRefresh);
            float currentDeadzone = GenericInput.FetchDeadzone();
            s.SetValues(currentDeadzone, 1f, 0f);
            SliderRefresh(currentDeadzone);
        }
        private void OnDestroy()
        {
            s.onValueChanged.RemoveAllListeners();
        }
        private void SliderRefresh(float f)
        {
            GenericInput.UpdateDeadzone(f);
            optionalText.text = storedString + ": " + f.Multiply(scalingFactor).ToInt().ToString();
        }
    }
}
