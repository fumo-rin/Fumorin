using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.SceneManagement;
namespace rinCore
{

    [InitializeOnLoad]
    public static class HierarchyShortcuts
    {
        private const string CreatedObjectName = "GameObject";

        static HierarchyShortcuts()
        {
            EditorApplication.hierarchyWindowItemOnGUI -= OnHierarchyGUI;
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
        }

        private static void OnHierarchyGUI(int instanceID, Rect selectionRect)
        {
            Event e = Event.current;
            if (e == null || e.type != EventType.KeyDown)
                return;

            if (!e.alt || e.shift || e.control || e.command)
                return;

            Transform[] selected = GetSelectedSceneTransforms();
            if (selected.Length == 0)
                return;

            switch (e.keyCode)
            {
                case KeyCode.UpArrow:
                    MoveSiblings(selected, -1);
                    e.Use();
                    break;

                case KeyCode.DownArrow:
                    MoveSiblings(selected, 1);
                    e.Use();
                    break;

                case KeyCode.LeftArrow:
                    CreateParentForSelection(selected);
                    e.Use();
                    break;

                case KeyCode.RightArrow:
                    CreateChildForSelection(selected);
                    e.Use();
                    break;
            }
        }

        private static Transform[] GetSelectedSceneTransforms()
        {
            if (Selection.transforms == null || Selection.transforms.Length == 0)
                return Array.Empty<Transform>();

            List<Transform> valid = new List<Transform>(Selection.transforms.Length);
            foreach (Transform t in Selection.transforms)
            {
                if (t == null)
                    continue;

                if (!t.gameObject.scene.IsValid() || !t.gameObject.scene.isLoaded)
                    continue;

                if ((t.hideFlags & HideFlags.HideInHierarchy) != 0)
                    continue;

                valid.Add(t);
            }

            return valid.Distinct().ToArray();
        }

        private static int GetSiblingCount(Transform parent, Scene scene)
        {
            if (parent != null)
                return parent.childCount;

            return scene.IsValid() ? scene.rootCount : 0;
        }

        private static void MoveSiblings(Transform[] selected, int delta)
        {
            List<Transform> ordered = delta < 0
                ? selected.OrderBy(t => t.GetSiblingIndex()).ToList()
                : selected.OrderByDescending(t => t.GetSiblingIndex()).ToList();

            bool movedAny = false;

            foreach (Transform t in ordered)
            {
                if (t == null)
                    continue;

                Transform parent = t.parent;
                Scene scene = t.gameObject.scene;
                int count = GetSiblingCount(parent, scene);
                if (count <= 1)
                    continue;

                int currentIndex = t.GetSiblingIndex();
                int targetIndex = Mathf.Clamp(currentIndex + delta, 0, count - 1);
                if (targetIndex == currentIndex)
                    continue;

                Undo.SetTransformParent(t, parent, "Move Hierarchy Object");
                t.SetSiblingIndex(targetIndex);
                movedAny = true;
            }

            if (movedAny)
            {
                EditorApplication.RepaintHierarchyWindow();
            }
        }

        private static void CreateParentForSelection(Transform[] selected)
        {
            Transform anchor = selected[0];
            if (anchor == null)
                return;

            Scene targetScene = anchor.gameObject.scene;
            Transform originalParent = anchor.parent;
            int originalSiblingIndex = anchor.GetSiblingIndex();
            Vector3 anchorPosition = anchor.position;

            GameObject newParent = new GameObject(CreatedObjectName);
            SceneManager.MoveGameObjectToScene(newParent, targetScene);
            Undo.RegisterCreatedObjectUndo(newParent, "Create Parent GameObject");

            Transform newParentTransform = newParent.transform;
            newParentTransform.SetParent(originalParent, false);
            newParentTransform.position = anchorPosition;

            int maxIndex = GetSiblingCount(originalParent, targetScene) - 1;
            newParentTransform.SetSiblingIndex(Mathf.Clamp(originalSiblingIndex, 0, Mathf.Max(0, maxIndex)));

            foreach (Transform t in selected)
            {
                if (t == null || t == newParentTransform || t.IsChildOf(newParentTransform))
                    continue;

                Undo.SetTransformParent(t, newParentTransform, "Parent Under New GameObject");
                t.SetParent(newParentTransform, true);
            }

            Selection.activeGameObject = newParent;
            EditorApplication.RepaintHierarchyWindow();
        }

        private static void CreateChildForSelection(Transform[] selected)
        {
            List<GameObject> created = new List<GameObject>(selected.Length);

            foreach (Transform parentTarget in selected)
            {
                if (parentTarget == null)
                    continue;

                GameObject newChild = new GameObject(CreatedObjectName);
                SceneManager.MoveGameObjectToScene(newChild, parentTarget.gameObject.scene);
                Undo.RegisterCreatedObjectUndo(newChild, "Create Child GameObject");

                Transform childTransform = newChild.transform;
                childTransform.SetParent(parentTarget, false);
                childTransform.position = parentTarget.position;

                created.Add(newChild);
            }

            if (created.Count > 0)
            {
                Selection.objects = created.Cast<UnityEngine.Object>().ToArray();
                Selection.activeGameObject = created[0];
                EditorApplication.RepaintHierarchyWindow();
            }
        }
    }
}
