using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace rinCore
{
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public class TypedComponentAttribute : Attribute
    {
        public string CustomCategory { get; }

        public TypedComponentAttribute(string customCategory = null)
        {
            CustomCategory = customCategory;
        }
    }
}

#if UNITY_EDITOR
namespace rinCore
{
    public static class TypedComponentAdder
    {
        private const string MENU_PATH = "Fumorin/Typed Component Adder";

        [MenuItem(MENU_PATH, true)]
        private static bool ValidateAddComponent()
        {
            return Selection.activeGameObject != null;
        }

        [MenuItem(MENU_PATH, false, 100)]
        private static void OpenComponentMenu()
        {
            GameObject selectedObj = Selection.activeGameObject;
            if (selectedObj == null) return;

            GenericMenu menu = new GenericMenu();

            var allMonoTypes = TypeCache.GetTypesDerivedFrom<MonoBehaviour>();

            foreach (Type type in allMonoTypes)
            {
                if (type.IsAbstract || type.IsGenericTypeDefinition)
                    continue;

                var attr = (TypedComponentAttribute)Attribute.GetCustomAttribute(type, typeof(TypedComponentAttribute), true);
                if (attr == null)
                    continue;

                string assemblyName = type.Assembly.GetName().Name;
                string baseTypeName = (type.BaseType != null && type.BaseType != typeof(MonoBehaviour))
                    ? type.BaseType.Name
                    : "Direct MonoBehaviours";

                string categoryPath = !string.IsNullOrEmpty(attr.CustomCategory)
                    ? attr.CustomCategory
                    : $"{assemblyName}/{baseTypeName}";

                string typeName = type.Name;
                string itemPath = $"{categoryPath}/{typeName}";

                bool alreadyHasComponent = selectedObj.GetComponent(type) != null;

                if (alreadyHasComponent)
                {
                    menu.AddDisabledItem(new GUIContent(itemPath), true);
                }
                else
                {
                    menu.AddItem(new GUIContent(itemPath), false, () =>
                    {
                        if (Selection.activeGameObject != null)
                        {
                            Undo.AddComponent(Selection.activeGameObject, type);
                        }
                    });
                }
            }

            menu.DropDown(new Rect(10, 10, 0, 0));
        }
    }
}
#endif