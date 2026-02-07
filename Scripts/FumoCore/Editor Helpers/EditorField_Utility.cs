using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace RinCore
{
    #region Managed Reference List (Dropdown List)
#if UNITY_EDITOR
    public static partial class EF_Utility
    {
        const float RemoveButtonWidth = 25f;
        public static void EF_TypeDropdownList<TBase>(Rect rect, string label, string listFieldName, UnityEngine.Object backingObject) where TBase : class
        {
            if (backingObject == null)
                return;

            const float RemoveButtonWidth = 25f;
            const float MoveButtonWidth = 22f;
            const float ButtonSpacing = 2f;

            SerializedObject so = new SerializedObject(backingObject);
            SerializedProperty listProp = so.FindProperty(listFieldName);
            if (listProp == null || !listProp.isArray)
            {
                EditorGUI.LabelField(new Rect(rect.x, rect.y, rect.width, RowHeight), $"Error: {listFieldName} not found or not an array");
                return;
            }

            so.Update();
            float y = rect.y;

            EditorGUI.LabelField(new Rect(rect.x, y, rect.width, RowHeight), label, EditorStyles.boldLabel);
            y += RowHeight + RowPadding;

            string GetNestedCategoryPath(Type type)
            {
                Stack<string> stack = new Stack<string>();
                Type current = type.DeclaringType;
                while (current != null)
                {
                    stack.Push(current.Name);
                    current = current.DeclaringType;
                }
                return stack.Count > 0 ? string.Join("/", stack) : "Uncategorized";
            }

            var derivedTypes = TypeCache.GetTypesDerivedFrom<TBase>()
                .Where(t => t.IsClass && !t.IsAbstract && t.IsSerializable)
                .Select(t => new { Type = t, Category = GetNestedCategoryPath(t) })
                .GroupBy(x => x.Category)
                .OrderBy(g => g.Key)
                .ToList();

            for (int i = 0; i < listProp.arraySize; i++)
            {
                SerializedProperty element = listProp.GetArrayElementAtIndex(i);
                object value = element.managedReferenceValue;

                string typeName = value != null ? value.GetType().Name : "(None)";

                float buttonsWidth = (MoveButtonWidth * 2) + RemoveButtonWidth + (ButtonSpacing * 3);

                Rect popupRect = new Rect(rect.x, y, rect.width - buttonsWidth, RowHeight);
                Rect upRect = new Rect(popupRect.xMax + ButtonSpacing, y, MoveButtonWidth, RowHeight);
                Rect downRect = new Rect(upRect.xMax + ButtonSpacing, y, MoveButtonWidth, RowHeight);
                Rect removeRect = new Rect(downRect.xMax + ButtonSpacing, y, RemoveButtonWidth, RowHeight);

                if (GUI.Button(popupRect, $"[{i}] {typeName}", EditorStyles.popup))
                {
                    GenericMenu menu = new GenericMenu();
                    menu.AddItem(new GUIContent("(None)"), value == null, () =>
                    {
                        element.managedReferenceValue = null;
                        so.ApplyModifiedProperties();
                        EditorUtility.SetDirty(backingObject);
                    });

                    foreach (var group in derivedTypes)
                    {
                        foreach (var entry in group.OrderBy(x => x.Type.Name))
                        {
                            Type captured = entry.Type;
                            string path = $"{group.Key}/{captured.Name}";
                            menu.AddItem(new GUIContent(path), value != null && value.GetType() == captured, () =>
                            {
                                element.managedReferenceValue = Activator.CreateInstance(captured);
                                so.ApplyModifiedProperties();
                                EditorUtility.SetDirty(backingObject);
                            });
                        }
                    }

                    menu.ShowAsContext();
                }

                GUI.enabled = i > 0;
                if (GUI.Button(upRect, "↑"))
                {
                    listProp.MoveArrayElement(i, i - 1);
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(backingObject);
                    GUIUtility.ExitGUI();
                }

                GUI.enabled = i < listProp.arraySize - 1;
                if (GUI.Button(downRect, "↓"))
                {
                    listProp.MoveArrayElement(i, i + 1);
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(backingObject);
                    GUIUtility.ExitGUI();
                }
                GUI.enabled = true;

                if (GUI.Button(removeRect, "−"))
                {
                    listProp.DeleteArrayElementAtIndex(i);
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(backingObject);
                    GUIUtility.ExitGUI();
                }

                y += RowHeight + RowPadding;

                if (element.managedReferenceValue != null)
                {
                    float drawerY = y;
                    EF_ClassDrawer(new Rect(rect.x + 15f, y, rect.width - 15f, 0f), element.managedReferenceValue, backingObject, ref drawerY);
                    y = drawerY + RowPadding;
                }
            }

            if (GUI.Button(new Rect(rect.x, y, rect.width, RowHeight), "+ Add"))
            {
                listProp.InsertArrayElementAtIndex(listProp.arraySize);
                listProp.GetArrayElementAtIndex(listProp.arraySize - 1).managedReferenceValue = null;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(backingObject);
            }

            so.ApplyModifiedProperties();
        }
        public static float GetEF_TypeDropdownListHeight<TBase>(string listFieldName, UnityEngine.Object backingObject)
        {
            if (backingObject == null) return 0;
            SerializedObject so = new SerializedObject(backingObject);
            SerializedProperty listProp = so.FindProperty(listFieldName);
            if (listProp == null) return 0;

            float height = RowHeight + RowPadding;
            for (int i = 0; i < listProp.arraySize; i++)
            {
                object obj = listProp.GetArrayElementAtIndex(i).managedReferenceValue;
                height += RowHeight + RowPadding + 10;
                height += EF_ClassDrawerHeight(obj);
                height += 5;
            }
            height += RowHeight + RowPadding;
            return height;
        }
    }
#endif
    #endregion
    #region Class Drawer
#if UNITY_EDITOR
    public static partial class EF_Utility
    {
        public static void EF_ClassDrawer(Rect startRect, object target, UnityEngine.Object backingObject, ref float yOffset)
        {
            if (target == null)
            {
                EditorGUI.LabelField(new Rect(startRect.x, yOffset, startRect.width, RowHeight), "(null)");
                yOffset += RowHeight + RowPadding;
                return;
            }

            Type type = target.GetType();
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);

            foreach (var field in fields)
            {
                if (field.IsNotSerialized) continue;
                if (Attribute.IsDefined(field, typeof(HideInInspector))) continue;

                string label = ObjectNames.NicifyVariableName(field.Name);
                Type fieldType = field.FieldType;
                object value = field.GetValue(target);

                Rect fieldRect = new Rect(startRect.x, yOffset, startRect.width, RowHeight);
                yOffset += RowHeight + RowPadding;

                EditorGUI.BeginChangeCheck();

                if (fieldType == typeof(float))
                    value = EditorGUI.FloatField(fieldRect, label, (float)(value ?? 0f));
                else if (fieldType == typeof(int))
                    value = EditorGUI.IntField(fieldRect, label, (int)(value ?? 0));
                else if (fieldType == typeof(bool))
                    value = EditorGUI.Toggle(fieldRect, label, (bool)(value ?? false));
                else if (fieldType == typeof(string))
                    value = EditorGUI.TextField(fieldRect, label, (string)(value ?? ""));
                else if (fieldType.IsEnum)
                    value = EditorGUI.EnumPopup(fieldRect, label, (Enum)(value ?? Activator.CreateInstance(fieldType)));
                else if (typeof(UnityEngine.Object).IsAssignableFrom(fieldType))
                    value = EditorGUI.ObjectField(fieldRect, label, (UnityEngine.Object)value, fieldType, true);
                else if (!fieldType.IsPrimitive && !fieldType.IsEnum && !fieldType.IsArray && fieldType.IsClass)
                {
                    EditorGUI.LabelField(fieldRect, $"{label} ({fieldType.Name})", EditorStyles.boldLabel);
                    if (value != null)
                    {
                        EditorGUI.indentLevel++;
                        EF_ClassDrawer(startRect, value, backingObject, ref yOffset);
                        EditorGUI.indentLevel--;
                    }
                }
                else
                {
                    EditorGUI.LabelField(fieldRect, label, $"({fieldType.Name}) Not Supported");
                }

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(backingObject, "Modify Field");
                    field.SetValue(target, value);
                    EditorUtility.SetDirty(backingObject);
                }
            }
        }

        public static float EF_ClassDrawerHeight(object target)
        {
            if (target == null) return RowHeight + RowPadding;

            Type type = target.GetType();
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
            float height = 0f;

            foreach (var field in fields)
            {
                if (field.IsNotSerialized) continue;
                if (Attribute.IsDefined(field, typeof(HideInInspector))) continue;

                height += RowHeight + RowPadding;
                Type fieldType = field.FieldType;
                if (!fieldType.IsPrimitive && !fieldType.IsEnum && !fieldType.IsArray && fieldType.IsClass && !typeof(UnityEngine.Object).IsAssignableFrom(fieldType))
                {
                    object value = field.GetValue(target);
                    height += EF_ClassDrawerHeight(value);
                }
            }
            return height;
        }
    }
#endif
    #endregion
    #region Editor Field List
#if UNITY_EDITOR
    public static partial class EF_Utility
    {
        private const float RowHeight = 20f;
        private const float RowPadding = 2f;
        private const float ButtonWidth = 25f;

        public static List<T> EF_ListField<T>(Rect rect, string label, List<T> list) where T : UnityEngine.Object
        {
            if (list == null)
                list = new List<T>();

            // Draw header label
            EditorGUI.LabelField(new Rect(rect.x, rect.y, rect.width, RowHeight), label);

            float yOffset = rect.y + RowHeight + RowPadding;

            for (int i = 0; i < list.Count; i++)
            {
                // Field area minus 3 buttons
                float fieldWidth = rect.width - (ButtonWidth * 3) - 10;
                Rect elementRect = new Rect(rect.x, yOffset, fieldWidth, RowHeight);

                // Up/Down/Remove buttons
                Rect upRect = new Rect(rect.x + fieldWidth + 5, yOffset, ButtonWidth, RowHeight);
                Rect downRect = new Rect(upRect.x + ButtonWidth + 2, yOffset, ButtonWidth, RowHeight);
                Rect removeRect = new Rect(downRect.x + ButtonWidth + 2, yOffset, ButtonWidth, RowHeight);

                // Draw object field
                list[i] = (T)EditorGUI.ObjectField(elementRect, GUIContent.none, list[i], typeof(T), false);

                // Up button
                GUI.enabled = i > 0;
                if (GUI.Button(upRect, "↑"))
                {
                    Swap(list, i, i - 1);
                    GUI.changed = true;
                }

                // Down button
                GUI.enabled = i < list.Count - 1;
                if (GUI.Button(downRect, "↓"))
                {
                    Swap(list, i, i + 1);
                    GUI.changed = true;
                }

                GUI.enabled = true;

                // Remove button
                if (GUI.Button(removeRect, "−"))
                {
                    list.RemoveAt(i);
                    GUI.changed = true;
                    break;
                }

                yOffset += RowHeight + RowPadding;
            }
            Rect addRect = new Rect(rect.x, yOffset, rect.width, RowHeight);
            if (GUI.Button(addRect, "+ Add"))
            {
                list.Add(null);
                GUI.changed = true;
            }

            return list;
        }

        public static float GetListFieldHeight<T>(string label, List<T> list) where T : UnityEngine.Object
        {
            int count = (list != null ? list.Count : 0);
            // 1 row for label + N rows + 1 row for add button
            return (count + 2) * (RowHeight + RowPadding);
        }

        private static void Swap<T>(List<T> list, int indexA, int indexB)
        {
            (list[indexB], list[indexA]) = (list[indexA], list[indexB]);
        }
    }
#endif
    #endregion
    #region Object Field
#if UNITY_EDITOR
    public static partial class EF_Utility
    {
        public static T EF_ObjectField<T>(Rect rect, string label, T current) where T : UnityEngine.Object
        {
            T result = (T)EditorGUI.ObjectField(rect, label, current, typeof(T), false);
            return result;
        }
    }
#endif
    #endregion
    #region Sliders
#if UNITY_EDITOR
    public static partial class EF_Utility
    {
        private static T EF_Slider_Internal<T>(Rect rect, string label, T value, T min, T max)
            where T : struct, IConvertible
        {
            T newValue;
            if (typeof(T) == typeof(float))
            {
                float result = EditorGUI.Slider(rect, label, Convert.ToSingle(value), Convert.ToSingle(min), Convert.ToSingle(max));
                newValue = (T)Convert.ChangeType(result, typeof(T));
            }
            else if (typeof(T) == typeof(int))
            {
                int result = EditorGUI.IntSlider(rect, label, Convert.ToInt32(value), Convert.ToInt32(min), Convert.ToInt32(max));
                newValue = (T)Convert.ChangeType(result, typeof(T));
            }
            else
            {
                throw new InvalidOperationException("EF_Slider only supports int or float types.");
            }

            return newValue;
        }
        public static float EF_Slider(Rect rect, string label, float value, float min, float max)
        {
            return EF_Slider_Internal(rect, label, value, min, max);
        }

        public static int EF_Slider(Rect rect, string label, int value, int min, int max)
        {
            return EF_Slider_Internal(rect, label, value, min, max);
        }
    }
#endif
    #endregion
    #region Bool Field
#if UNITY_EDITOR
    public static partial class EF_Utility
    {
        public static bool EF_BoolField(Rect rect, string label, bool value)
        {
            bool newValue = EditorGUI.Toggle(rect, label, value);
            return newValue;
        }
    }
#endif
    #endregion
    #region Button
    public static partial class EF_Utility
    {
        public static bool EF_Button(Rect rect, string label)
        {
            if (Event.current != null && Event.current.isMouse)
            {
                if (Event.current.button != 0)
                    return false;
            }
            return GUI.Button(rect, label);
        }
    }
    #endregion
    #region Class Dropdown
#if UNITY_EDITOR
    public partial class EF_Utility
    {
        public static TBase EF_TypeDropdown<TBase>(Rect rect, string label, TBase currentValue)
        where TBase : class
        {
            Type baseType = typeof(TBase);

            var types = TypeCache.GetTypesDerivedFrom(baseType)
                .Where(t => !t.IsAbstract && t.IsClass && t.IsSerializable)
                .OrderBy(t => t.Name)
                .ToList();
            List<string> names = new() { "(None)" };
            names.AddRange(types.Select(t => t.Name));

            int currentIndex = 0;
            if (currentValue != null)
            {
                var currentType = currentValue.GetType();
                currentIndex = types.FindIndex(t => t == currentType) + 1;
            }
            int newIndex = EditorGUI.Popup(rect, label, currentIndex, names.ToArray());

            if (newIndex == currentIndex)
                return currentValue;

            if (newIndex <= 0)
                return null;

            var selectedType = types[newIndex - 1];
            try
            {
                return Activator.CreateInstance(selectedType) as TBase;
            }
            catch (Exception ex)
            {
                Debug.LogError($"EF_TypeDropdown<{baseType.Name}>: Failed to instantiate {selectedType}: {ex}");
                return currentValue;
            }
        }
    }
#endif
    #endregion
    #region Sprite Field
    public static partial class EF_Utility
    {
        private const float SpritePreviewSize = 40f;

#if UNITY_EDITOR
        public static Sprite EF_SpriteField(Rect rect, string label, Sprite current)
        {
            Rect labelRect = new Rect(rect.x, rect.y, EditorGUIUtility.labelWidth, rect.height);
            EditorGUI.LabelField(labelRect, label);

            Rect previewRect = new Rect(labelRect.xMax, rect.y, SpritePreviewSize, rect.height);

            if (current != null)
            {
                EditorGUI.DrawPreviewTexture(previewRect, current.texture, null, ScaleMode.ScaleToFit);
            }
            else
            {
                EditorGUI.DrawRect(previewRect, new Color(0.3f, 0.3f, 0.3f, 1f));
            }

            Rect fieldRect = new Rect(previewRect.xMax + 5, rect.y, rect.width - SpritePreviewSize - EditorGUIUtility.labelWidth - 10, rect.height);
            current = (Sprite)EditorGUI.ObjectField(fieldRect, GUIContent.none, current, typeof(Sprite), false);

            return current;
        }
#endif
    }
    #endregion
}