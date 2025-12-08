using System.IO;
using UnityEditor;
using UnityEngine;

namespace RinCore
{
#if UNITY_EDITOR
    public static partial class FCEHelper
    {
        public static void CreateFolders(string path)
        {
            string[] parts = path.Split('/');
            if (parts.Length == 0 || parts[0] != "Assets")
            {
                Debug.LogError("Invalid folder path. Must start with 'Assets'.");
                return;
            }

            string current = "Assets";
            for (int i = 1; i < parts.Length; i++)
            {
                string next = Path.Combine(current, parts[i]);
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }
    }
#endif
}
