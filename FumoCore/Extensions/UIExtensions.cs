using UnityEngine;
using UnityEngine.UI;

namespace FumoCore.Tools
{
    public static class SliderExtensions
    {
        public static Slider SetValues(this Slider s, float value, float maxValue, float minValue)
        {
            s.maxValue = maxValue;
            s.value = value;
            s.minValue = minValue;
            return s;
        }
        public static Slider SetValuesInt(this Slider s, int value, int maxValue, int minValue)
        {
            s.wholeNumbers = true;
            s.maxValue = maxValue;
            s.value = value;
            s.minValue = minValue;
            return s;
        }
        public static void SetXPositionOfRect(this Slider slider, RectTransform target, float percent)
        {
            if (slider == null || target == null)
                return;

            percent = Mathf.Clamp01(percent);

            RectTransform sliderRect = slider.fillRect != null ? slider.fillRect : slider.GetComponent<RectTransform>();
            float sliderWidth = sliderRect.rect.width;

            float newX = Mathf.Lerp(0f, sliderWidth, percent);

            float pivotOffset = sliderWidth * sliderRect.pivot.x;
            newX -= pivotOffset;

            Vector2 anchoredPos = target.anchoredPosition;
            anchoredPos.x = newX;
            target.anchoredPosition = anchoredPos;
        }
    }
}
