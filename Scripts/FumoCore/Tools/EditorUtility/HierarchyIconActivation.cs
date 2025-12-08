using UnityEngine;
using UnityEditor;

namespace RinCore
{
#if UNITY_EDITOR
    [InitializeOnLoad]
    public static class HierarchyIconActivation
    {
        static HierarchyIconActivation()
        {
            EditorApplication.hierarchyWindowItemOnGUI += PressItem;
        }
        private static void PressItem(int instanceID, Rect selection)
        {
            GameObject obj = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
            if (obj == null) return;
            Rect rect = new Rect(selection.x, selection.y, 15f, selection.height);
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && rect.Contains(Event.current.mousePosition))
            {
                Undo.RecordObject(obj, "Changing acitve state of object");
                obj.SetActive(!obj.activeSelf);
                Event.current.Use();
            }
        }
    }
#endif
}