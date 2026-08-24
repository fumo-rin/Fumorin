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

    [AttributeUsage(AttributeTargets.Field)]
    public class ManagedReferenceListAttribute : PropertyAttribute
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

            float labelWidth = EditorGUIUtility.labelWidth - 15;
            Rect labelRect = new Rect(line.x + 15, line.y, labelWidth, line.height);
            EditorGUI.LabelField(labelRect, label);

            Rect dropdown = new Rect(line.x + 15 + labelWidth, line.y, line.width - (15 + labelWidth), line.height);

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

                Type baseType = GetElementType(fieldInfo.FieldType);

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

        public static Type GetElementType(Type fieldType)
        {
            if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>))
            {
                return fieldType.GetGenericArguments()[0];
            }
            if (fieldType.IsArray)
            {
                return fieldType.GetElementType();
            }
            return fieldType;
        }

        public static string GetNestedMenuPath(Type type)
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

    [CustomPropertyDrawer(typeof(ManagedReferenceListAttribute), true)]
    public class ManagedReferenceListDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isArray)
                return EditorGUI.GetPropertyHeight(property, label, true);

            float h = EditorGUIUtility.singleLineHeight;

            if (property.isExpanded)
            {
                h += EditorGUIUtility.singleLineHeight + 4; // Add/Clear buttons
                for (int i = 0; i < property.arraySize; i++)
                {
                    SerializedProperty element = property.GetArrayElementAtIndex(i);
                    h += GetElementHeight(element) + 4;
                }
            }

            return h;
        }

        private float GetElementHeight(SerializedProperty element)
        {
            float h = EditorGUIUtility.singleLineHeight;
            if (element.managedReferenceValue != null && element.isExpanded)
            {
                var copy = element.Copy();
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
            if (!property.isArray)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            float lineHeight = EditorGUIUtility.singleLineHeight;
            Rect headerRect = new Rect(position.x, position.y, position.width, lineHeight);

            property.isExpanded = EditorGUI.Foldout(headerRect, property.isExpanded, label, true);

            if (!property.isExpanded)
                return;

            EditorGUI.indentLevel++;

            float y = position.y + lineHeight + 2;

            // Draw Add button with dynamic class menu
            Rect addBtnRect = new Rect(position.x + 15, y, position.width - 15, lineHeight);
            if (GUI.Button(addBtnRect, "+ Add New Element"))
            {
                Type baseType = BaseGunDrawer.GetElementType(fieldInfo.FieldType);

                GenericMenu menu = new();
                var derivedTypes = TypeCache.GetTypesDerivedFrom(baseType)
                    .Where(t => !t.IsAbstract && !t.IsInterface && t.GetConstructor(Type.EmptyTypes) != null);

                foreach (var type in derivedTypes)
                {
                    string path = BaseGunDrawer.GetNestedMenuPath(type);
                    menu.AddItem(new GUIContent(path), false, () =>
                    {
                        property.serializedObject.Update();
                        property.arraySize++;
                        SerializedProperty newElem = property.GetArrayElementAtIndex(property.arraySize - 1);
                        newElem.managedReferenceValue = Activator.CreateInstance(type);
                        property.serializedObject.ApplyModifiedProperties();
                    });
                }
                menu.ShowAsContext();
            }

            y += lineHeight + 4;

            // Draw elements inside list
            for (int i = 0; i < property.arraySize; i++)
            {
                SerializedProperty element = property.GetArrayElementAtIndex(i);
                float elemHeight = GetElementHeight(element);
                Rect elemRect = new Rect(position.x, y, position.width - 30, elemHeight);

                // Draw standard picker layout fow single element
                DrawElementPicker(elemRect, element, new GUIContent($"Item {i}"));

                // Draw Remove button on the side
                Rect removeBtnRect = new Rect(position.x + position.width - 25, y, 25, lineHeight);
                if (GUI.Button(removeBtnRect, "X"))
                {
                    property.serializedObject.Update();
                    property.DeleteArrayElementAtIndex(i);
                    property.serializedObject.ApplyModifiedProperties();
                    break;
                }

                y += elemHeight + 4;
            }

            EditorGUI.indentLevel--;
        }

        private void DrawElementPicker(Rect position, SerializedProperty property, GUIContent label)
        {
            Rect line = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

            property.isExpanded = EditorGUI.Foldout(
                new Rect(line.x, line.y, 15, line.height),
                property.isExpanded,
                GUIContent.none);

            float labelWidth = EditorGUIUtility.labelWidth - 30;
            Rect labelRect = new Rect(line.x + 15, line.y, labelWidth, line.height);
            EditorGUI.LabelField(labelRect, label);

            Rect dropdown = new Rect(line.x + 15 + labelWidth, line.y, line.width - (15 + labelWidth), line.height);

            string current = property.managedReferenceValue == null
                ? "None"
                : BaseGunDrawer.GetNestedMenuPath(property.managedReferenceValue.GetType());

            if (EditorGUI.DropdownButton(dropdown, new GUIContent(current), FocusType.Passive))
            {
                GenericMenu menu = new();

                menu.AddItem(new GUIContent("None"), property.managedReferenceValue == null, () =>
                {
                    property.serializedObject.Update();
                    property.managedReferenceValue = null;
                    property.serializedObject.ApplyModifiedProperties();
                });

                Type baseType = BaseGunDrawer.GetElementType(fieldInfo.FieldType);
                var derivedTypes = TypeCache.GetTypesDerivedFrom(baseType)
                    .Where(t => !t.IsAbstract && !t.IsInterface && t.GetConstructor(Type.EmptyTypes) != null);

                foreach (var type in derivedTypes)
                {
                    string path = BaseGunDrawer.GetNestedMenuPath(type);
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
    }
#endif
    #endregion
}