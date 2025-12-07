using UnityEditor;
using UnityEngine;

namespace RinCore
{
#if UNITY_EDITOR
    public static partial class FCEHelper
    {
        public static void CreateAsset(Object o, string path)
        {
            AssetDatabase.CreateAsset(o, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = o;
        }
    }
#endif
}
