
using UnityEngine;

namespace rinCore
{
#if UNITY_EDITOR
    using UnityEditor;
    [CustomPropertyDrawer(typeof(ShowIfAttribute))]
    public class ShowIfDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var showIf = (ShowIfAttribute)attribute;

            SerializedProperty condition =
                property.serializedObject.FindProperty(showIf.Condition);

            if (condition != null && condition.boolValue)
                EditorGUI.PropertyField(position, property, label, true);
        }

        public override float GetPropertyHeight(
            SerializedProperty property,
            GUIContent label)
        {
            var showIf = (ShowIfAttribute)attribute;

            SerializedProperty condition =
                property.serializedObject.FindProperty(showIf.Condition);

            return condition != null && condition.boolValue
                ? EditorGUI.GetPropertyHeight(property, label, true)
                : 0f;
        }
    }
#endif
    public class ShowIfAttribute : PropertyAttribute
    {
        public readonly string Condition;

        public ShowIfAttribute(string condition)
        {
            Condition = condition;
        }
    }
}