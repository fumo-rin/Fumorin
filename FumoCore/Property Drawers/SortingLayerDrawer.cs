using UnityEngine;
using UnityEditor;
#if UNITY_EDITOR
using UnityEditorInternal;
#endif
namespace FumoCore.Tools
{
    public class SortingLayerAttribute : PropertyAttribute
    {

    }
#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(SortingLayerAttribute))]
    public class SortingLayerDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.LabelField(position, label.text, "Use [SortingLayer] with a string.");
                return;
            }

            string[] sortingLayerNames = GetSortingLayerNames();
            int currentIndex = Mathf.Max(0, System.Array.IndexOf(sortingLayerNames, property.stringValue));

            int selectedIndex = EditorGUI.Popup(position, label.text, currentIndex, sortingLayerNames);
            property.stringValue = sortingLayerNames[selectedIndex];
        }
        private static string[] GetSortingLayerNames()
        {
            var sortingLayersProperty = typeof(InternalEditorUtility).GetProperty(
                "sortingLayerNames",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static
            );

            return sortingLayersProperty?.GetValue(null, null) as string[] ?? new string[0];
        }
    }
#endif
}