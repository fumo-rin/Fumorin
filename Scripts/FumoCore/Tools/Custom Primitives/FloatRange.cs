namespace RinCore
{
    using System;
    using UnityEngine;

#if UNITY_EDITOR
    using UnityEditor;
    using System.Reflection;

    [CustomPropertyDrawer(typeof(FloatRange))]
    public class FloatRangeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty minProp = property.FindPropertyRelative("min");
            SerializedProperty maxProp = property.FindPropertyRelative("max");

            float min = minProp.floatValue;
            float max = maxProp.floatValue;

            RangeSliderAttribute rangeAttr = fieldInfo.GetCustomAttribute<RangeSliderAttribute>();
            float sliderMin = rangeAttr != null ? rangeAttr.MinLimit : 0f;
            float sliderMax = rangeAttr != null ? rangeAttr.MaxLimit : 1f;

            Rect labelRect = new(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            Rect sliderRect = new(position.x, position.y + 16, position.width, EditorGUIUtility.singleLineHeight);
            Rect minFieldRect = new(position.x, position.y + 32, position.width / 2 - 2, EditorGUIUtility.singleLineHeight);
            Rect maxFieldRect = new(position.x + position.width / 2 + 2, position.y + 32, position.width / 2 - 2, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(labelRect, label);
            EditorGUI.MinMaxSlider(sliderRect, ref min, ref max, sliderMin, sliderMax);

            min = Mathf.Clamp(EditorGUI.FloatField(minFieldRect, min), sliderMin, max);
            max = Mathf.Clamp(EditorGUI.FloatField(maxFieldRect, max), min, sliderMax);

            minProp.floatValue = min;
            maxProp.floatValue = max;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight * 3 + 4;
        }
    }
#endif

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class RangeSliderAttribute : PropertyAttribute
    {
        public float MinLimit { get; private set; }
        public float MaxLimit { get; private set; }

        public RangeSliderAttribute(float minLimit, float maxLimit)
        {
            MinLimit = minLimit;
            MaxLimit = maxLimit;
        }
    }

    [Serializable]
    public class FloatRange
    {
        public float min;
        public float max;

        public FloatRange(float min, float max)
        {
            this.min = min;
            this.max = max;
        }
        public float Min => Mathf.Min(min, max);
        public float Max => Mathf.Max(min, max);

        public float Random => UnityEngine.Random.Range(Min, Max);
        public int RandomInt => UnityEngine.Random.Range(Mathf.RoundToInt(Min), Mathf.RoundToInt(Max + 1));
        public int ToInt() => Mathf.RoundToInt(Random);

        public float Lerp(float lerp) => Mathf.Lerp(Min, Max, lerp);
    }
}
