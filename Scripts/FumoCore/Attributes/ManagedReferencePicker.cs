using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace rinCore
{
    [AttributeUsage(AttributeTargets.Field)]
    public class ManagedReferencePickerAttribute : PropertyAttribute
    {
    }

    #region Prop Drawer
#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(ManagedReferencePickerAttribute), true)]
    public class BaseGunDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float h = EditorGUIUtility.singleLineHeight;

            if (property.managedReferenceValue != null && property.isExpanded)
            {
                var copy = property.Copy();
                var end = copy.GetEndProperty();

                bool enterChildren = true;
                while (copy.NextVisible(enterChildren) && !SerializedProperty.EqualContents(copy, end))
                {
                    h += EditorGUI.GetPropertyHeight(copy, true) + 2;
                    enterChildren = false;
                }
            }

            return h;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Rect line = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

            property.isExpanded = EditorGUI.Foldout(
                new Rect(line.x, line.y, 15, line.height),
                property.isExpanded,
                GUIContent.none);

            // 1. Dwwaw de pwwopewty wabel (e.g., "Use Action")
            float labelWidth = EditorGUIUtility.labelWidth - 15;
            Rect labelRect = new Rect(line.x + 15, line.y, labelWidth, line.height);
            EditorGUI.LabelField(labelRect, label);

            // 2. Dwwaw de dwopdown button next to de wabel
            Rect dropdown = new Rect(line.x + 15 + labelWidth, line.y, line.width - (15 + labelWidth), line.height);

            // Formatting de active menu text to show de fuww path (e.g. "ItemTesting/DebugItemAction")
            string current = property.managedReferenceValue == null
                ? "None"
                : GetNestedMenuPath(property.managedReferenceValue.GetType());

            if (EditorGUI.DropdownButton(dropdown, new GUIContent(current), FocusType.Passive))
            {
                GenericMenu menu = new();

                menu.AddItem(new GUIContent("None"), property.managedReferenceValue == null, () =>
                {
                    property.serializedObject.Update();
                    property.managedReferenceValue = null;
                    property.serializedObject.ApplyModifiedProperties();
                });

                Type baseType = fieldInfo.FieldType;
                if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    baseType = baseType.GetGenericArguments()[0];
                }
                else if (baseType.IsArray)
                {
                    baseType = baseType.GetElementType();
                }

                var derivedTypes = TypeCache.GetTypesDerivedFrom(baseType)
                    .Where(t => !t.IsAbstract && !t.IsInterface && t.GetConstructor(Type.EmptyTypes) != null);

                foreach (var type in derivedTypes)
                {
                    string path = GetNestedMenuPath(type);

                    menu.AddItem(new GUIContent(path), false, () =>
                    {
                        property.serializedObject.Update();
                        property.managedReferenceValue = Activator.CreateInstance(type);
                        property.serializedObject.ApplyModifiedProperties();
                    });
                }

                menu.ShowAsContext();
            }

            if (!property.isExpanded || property.managedReferenceValue == null)
                return;

            EditorGUI.indentLevel++;

            float y = line.yMax + 2;

            var copy = property.Copy();
            var end = copy.GetEndProperty();

            bool enterChildren = true;
            while (copy.NextVisible(enterChildren) && !SerializedProperty.EqualContents(copy, end))
            {
                float h = EditorGUI.GetPropertyHeight(copy, true);

                EditorGUI.PropertyField(
                    new Rect(position.x, y, position.width, h),
                    copy,
                    true);

                y += h + 2;
                enterChildren = false;
            }

            EditorGUI.indentLevel--;
        }

        private static string GetNestedMenuPath(Type type)
        {
            Stack<string> path = new();

            Type current = type;
            while (current != null)
            {
                path.Push(current.Name);
                current = current.DeclaringType;
            }

            return string.Join("/", path);
        }
    }
#endif
    #endregion
}