using System;
using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace rinCore
{
    [DefaultExecutionOrder(-50)]
    public class RunescapeDock : MonoBehaviour, IHierarchyComponentColor
    {
        #region Auto Naming Context Menu
#if UNITY_EDITOR
        [ContextMenu("Auto Name & Set Dirty")]
        private void AutoNameAndSetDirty()
        {
            int modifiedCount = 0;

            foreach (var entry in selectors)
            {
                if (entry.dockSelection != null)
                {
                    string newName = $"{entry.item.ToSpacedString()}# Dock";
                    if (entry.dockSelection.gameObject.name != newName)
                    {
                        Undo.RecordObject(entry.dockSelection.gameObject, "Rename DockSelector GameObject");
                        entry.dockSelection.gameObject.name = newName;
                        EditorUtility.SetDirty(entry.dockSelection.gameObject);
                        modifiedCount++;
                    }
                }
            }

            if (modifiedCount > 0)
            {
                Undo.RecordObject(gameObject, "Auto Name Dock Selectors");
                EditorUtility.SetDirty(gameObject);
                Debug.Log($"S-successfuwwy wenamed and set diwty {modifiedCount} DockSelector GameObjects! OwO");
            }
            else
            {
                Debug.Log("Nodin needed w-wenaming! uwu");
            }
        }
#endif
        #endregion
        public Color LabelColor => ColorHelper.PastelBlue.Opacity(50);
        #region Dock Item & Selector Fill
        public enum DockItem
        {
            Inventory = 100,
            Gear = 200,
            Prayer = 300,
            Magic = 400,
            Placeholder1 = 500,
            Placeholder2 = 600,
            Stats = 700,
            Quests = 800,
            Settings = 900,
            Music = 1000,
            Placeholder3 = 1100,
            ExitGame = 1200,
        }
        [System.Serializable]
        public class Entry
        {
            public DockItem item;
            public RunescapeDockSelector dockSelection;
            public GameObject nestActivation;

            public Entry(DockItem item)
            {
                this.item = item;
                dockSelection = null;
                nestActivation = null;
            }
        }
        [SerializeField]
        List<Entry> selectors = new(12)
        {
            new(DockItem.Inventory),
            new(DockItem.Gear),
            new(DockItem.Prayer),
            new(DockItem.Magic),
            new(DockItem.Placeholder1),
            new(DockItem.Placeholder2),
            new(DockItem.Stats),
            new(DockItem.Quests),
            new(DockItem.Settings),
            new(DockItem.Music),
            new(DockItem.Placeholder3),
            new(DockItem.ExitGame),
        };
        #endregion
        static DockItem? currentSelection;
        private void Awake()
        {
            EventBus.Bind<RunescapeDockSelector.Runescape_Dock_Selection>(SelectionAction);
        }
        private void Start()
        {
            foreach (var selector in selectors)
            {
                if (selector.dockSelection != null)
                {
                    selector.dockSelection.TabSelection_WithSideEffects = selector.item;
                }
            }
            DockItem fetchedSelection = currentSelection ?? DockItem.Inventory;
            new RunescapeDockSelector.Runescape_Dock_Selection(fetchedSelection).Publish();
        }
        private void OnDestroy()
        {
            EventBus.Release<RunescapeDockSelector.Runescape_Dock_Selection>(SelectionAction);
        }
        void SelectionAction(RunescapeDockSelector.Runescape_Dock_Selection action)
        {
            var enumSelection = action.item;
            currentSelection = enumSelection;

            foreach (var entry in selectors)
            {
                var iteration = entry.dockSelection;
                if (iteration == null)
                {
                    Debug.LogError("Bad");
                    continue;
                }
                bool isIterationSelected = entry.item == enumSelection;
                if (entry.nestActivation != null)
                    entry.nestActivation.SetActive(isIterationSelected);

                if (enumSelection == entry.dockSelection.TabSelection_WithSideEffects)
                {
                    iteration.Select(null);
                    continue;
                }
                iteration.Unselect();
            }
        }
    }
}