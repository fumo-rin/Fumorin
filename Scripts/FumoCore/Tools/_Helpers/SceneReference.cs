using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RinCore
{
    [System.Serializable]
    public class SceneReference
    {
#if UNITY_EDITOR
        [SerializeField] public SceneAsset sceneAsset;
#endif
        [SerializeField] private string scenePath = string.Empty;
        public string GetSceneName()
        {
            if (string.IsNullOrEmpty(scenePath))
                return string.Empty;
            return System.IO.Path.GetFileNameWithoutExtension(scenePath);
        }
        public int GetBuildIndex()
        {
            if (string.IsNullOrEmpty(scenePath))
                return -1;

            int sceneCount = SceneManager.sceneCountInBuildSettings;
            for (int i = 0; i < sceneCount; i++)
            {
                string path = SceneUtility.GetScenePathByBuildIndex(i);
                if (path == scenePath)
                    return i;
            }
            return -1;
        }
        public static implicit operator string(SceneReference reference)
        {
            return reference?.GetSceneName() ?? string.Empty;
        }
        public static implicit operator int(SceneReference reference)
        {
            return reference?.GetBuildIndex() ?? -1;
        }
    }

#if UNITY_EDITOR

    [CustomPropertyDrawer(typeof(RinCore.SceneReference))]
    public class SceneReferenceDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var sceneAssetProp = property.FindPropertyRelative("sceneAsset");
            var scenePathProp = property.FindPropertyRelative("scenePath");

            position = EditorGUI.PrefixLabel(position, label);
            EditorGUI.PropertyField(position, sceneAssetProp, GUIContent.none);

            if (sceneAssetProp.objectReferenceValue != null)
            {
                string newPath = AssetDatabase.GetAssetPath(sceneAssetProp.objectReferenceValue);
                if (scenePathProp.stringValue != newPath)
                {
                    scenePathProp.stringValue = newPath;
                    property.serializedObject.ApplyModifiedProperties();
                }
            }
            else if (!string.IsNullOrEmpty(scenePathProp.stringValue))
            {
                scenePathProp.stringValue = string.Empty;
                property.serializedObject.ApplyModifiedProperties();
            }

            EditorGUI.EndProperty();
        }
    }
#endif
}
