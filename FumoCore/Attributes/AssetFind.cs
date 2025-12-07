using UnityEngine;

namespace RinCore
{
#if UNITY_EDITOR
    using UnityEditor;
    using System;

    [CustomEditor(typeof(UnityEngine.Object), true)]
    public class AssetFindEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var type = target.GetType();
            var attr = (AssetFindAttribute)Attribute.GetCustomAttribute(type, typeof(AssetFindAttribute));

            if (attr != null)
            {
                if (GUILayout.Button(attr.ButtonLabel, GUILayout.Height(25)))
                {
                    EditorGUIUtility.PingObject(target);
                    Selection.activeObject = target;
                }
            }
        }
    }
#endif
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public class AssetFindAttribute : PropertyAttribute
    {
        public string ButtonLabel { get; }

        public AssetFindAttribute(string buttonLabel = "Find")
        {
            ButtonLabel = buttonLabel;
        }
    }
}
