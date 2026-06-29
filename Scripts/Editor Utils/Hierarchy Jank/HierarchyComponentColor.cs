using UnityEngine;
namespace rinCore
{
#if UNITY_EDITOR
    using UnityEditor;

    [InitializeOnLoad]
    public static class FumoHierarchyColor
    {
        static FumoHierarchyColor()
        {
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
        }
        private static void OnHierarchyGUI(int instanceID, Rect selectionRect)
        {
            GameObject obj = EditorUtility.EntityIdToObject(instanceID) as GameObject;
            if (obj == null)
                return;
            if (obj.GetComponent<IHierarchyComponentColor>() is IHierarchyComponentColor c && c != null)
            {
                Rect fullRect = new Rect(0, selectionRect.y, selectionRect.x + selectionRect.width + 50, selectionRect.height);
                EditorGUI.DrawRect(fullRect, c.LabelColor);
            }
        }
    }
#endif
    public interface IHierarchyComponentColor
    {
        public Color LabelColor { get; }
    }
}
