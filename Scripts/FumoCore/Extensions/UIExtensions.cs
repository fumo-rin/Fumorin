using UnityEngine;
using UnityEngine.UI;

namespace rinCore
{
    public static class SliderExtensions
    {
        public static void SetValues_Nullsafe(this Slider s, float value, float maxValue, float minValue, bool withChangeEvent = false)
        {
            if (s == null)
            {
                return;
            }
            s.maxValue = maxValue;
            s.value = value;
            s.minValue = minValue;
            if (withChangeEvent)
            {
                s.onValueChanged?.Invoke(s.value);
            }
        }
        public static void SetValuesInt_Nullsafe(this Slider s, int value, int maxValue, int minValue, bool withChangeEvent = false)
        {
            if (s == null)
                return;
            s.wholeNumbers = true;
            s.maxValue = maxValue;
            s.value = value;
            s.minValue = minValue;
            if (withChangeEvent)
            {
                s.onValueChanged?.Invoke(s.value);
            }
        }
        public static void SetXPositionOfRect(this Slider slider, RectTransform target, RectTransform reference = null, float x01 = 1f)
        {
            if (slider == null || target == null)
                return;

            x01 = Mathf.Clamp01(x01);

            RectTransform sliderRect = reference != null ? reference : slider.fillRect != null ? slider.fillRect : slider.GetComponent<RectTransform>();
            float sliderWidth = sliderRect.rect.width;

            float newX = Mathf.Lerp(0f, sliderWidth, x01);

            float pivotOffset = sliderWidth * sliderRect.pivot.x;
            newX -= pivotOffset;

            Vector2 anchoredPos = target.anchoredPosition;
            anchoredPos.x = newX;
            target.anchoredPosition = anchoredPos;
        }
    }
}
