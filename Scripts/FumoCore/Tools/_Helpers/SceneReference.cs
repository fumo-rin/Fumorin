using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace rinCore
{
#if UNITY_EDITOR
    using UnityEditor;
    using UnityEngine;

    namespace rinCore.Editor
    {
        [CustomPropertyDrawer(typeof(SceneReference))]
        public class SceneReferencePropertyDrawer : PropertyDrawer
        {
            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                EditorGUI.BeginProperty(position, label, property);

                SerializedProperty sceneAssetProp = property.FindPropertyRelative("sceneAsset");
                SerializedProperty scenePathProp = property.FindPropertyRelative("scenePath");

                EditorGUI.BeginChangeCheck();
                Object newScene = EditorGUI.ObjectField(position, label, sceneAssetProp.objectReferenceValue, typeof(SceneAsset), false);

                if (EditorGUI.EndChangeCheck())
                {
                    sceneAssetProp.objectReferenceValue = newScene;
                    if (newScene != null)
                    {
                        scenePathProp.stringValue = AssetDatabase.GetAssetPath(newScene);
                    }
                    else
                    {
                        scenePathProp.stringValue = string.Empty;
                    }
                }

                EditorGUI.EndProperty();
            }
        }
    }
#endif
    [System.Serializable]
    public class SceneReference : ISerializationCallbackReceiver
    {
#if UNITY_EDITOR
        [SerializeField] private SceneAsset sceneAsset;
#endif
        [SerializeField] private string scenePath = string.Empty;

        public string ScenePath => scenePath;

        public bool IsValid => !string.IsNullOrEmpty(scenePath);

        public string GetSceneName()
        {
            if (string.IsNullOrEmpty(scenePath)) return string.Empty;
            int lastSlash = scenePath.LastIndexOf('/');
            int lastDot = scenePath.LastIndexOf('.');
            if (lastSlash >= 0 && lastDot > lastSlash)
                return scenePath.Substring(lastSlash + 1, lastDot - lastSlash - 1);
            return scenePath;
        }

        public void OnBeforeSerialize()
        {
#if UNITY_EDITOR
            if (sceneAsset != null)
            {
                string path = AssetDatabase.GetAssetPath(sceneAsset);
                if (path != scenePath)
                {
                    scenePath = path;
                }
            }
            else
            {
                scenePath = string.Empty;
            }
#endif
        }

        public void OnAfterDeserialize() { }
    }
}