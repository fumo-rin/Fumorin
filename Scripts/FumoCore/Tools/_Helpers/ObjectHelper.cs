using System;
using System.Collections;
using UnityEditor;
using UnityEngine;

namespace RinCore
{
    public static partial class RinHelper
    {
        public static void EditorPing(this UnityEngine.Object o)
        {
#if UNITY_EDITOR
            UnityEditor.EditorGUIUtility.PingObject(o);
#endif
        }
        public static void Repaint()
        {
#if UNITY_EDITOR
            SceneView.RepaintAll();
#endif
        }
        public static void SetDirtyAndSave(this UnityEngine.Object o)
        {
#if UNITY_EDITOR
            EditorUtility.SetDirty(o);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
#endif
        }
        public static void Dirty(this UnityEngine.Object o)
        {
#if UNITY_EDITOR
            EditorUtility.SetDirty(o);
#endif
        }
        public static T Spawn2D<T>(this T reference, Vector2 position, Transform parent = null) where T : UnityEngine.MonoBehaviour
        {
            if (reference == null)
            {
                return default(T);
            }
            T spawn = MonoBehaviour.Instantiate(reference, position, Quaternion.identity);
            if (parent != null)
            {
                spawn.transform.SetParent(parent);
            }
            return spawn;
        }
#if UNITY_EDITOR
        public static bool TryGetAssetByPath<T>(string path, out T asset) where T : UnityEngine.Object
        {
            asset = null;
            if (AssetDatabase.AssetPathExists(path) && AssetDatabase.LoadAssetAtPath<T>(path) is T result)
            {
                asset = result;
            }
            return asset != null;
        }
#endif
    }
}