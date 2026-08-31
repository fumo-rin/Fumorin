using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace rinCore
{
    public class RunescapeDockSelector : MonoBehaviour, IPointerDownHandler, IHierarchyComponentColor
    {
        public Color LabelColor => ColorHelper.PastelBlue.Opacity(25);
        [SerializeField] Image selectionLabel;
        [SerializeField] TMP_Text labelText;
        RunescapeDock.DockItem _tabBackingField;
        public RunescapeDock.DockItem TabSelection_WithSideEffects
        {
            get
            {
                return _tabBackingField;
            }
            set
            {
                _tabBackingField = value;

                if (labelText is TMP_Text text && text.gameObject != null)
                {
                    text.text = value.ToSpacedString();
                }
            }
        }


        public record Runescape_Dock_Selection(RunescapeDock.DockItem item);
        public void OnPointerDown(PointerEventData eventData)
        {
            new Runescape_Dock_Selection(TabSelection_WithSideEffects).Publish();
        }
        public void Select(Action extraAction)
        {
            selectionLabel.enabled = true;
        }
        public void Unselect()
        {
            selectionLabel.enabled = false;
        }
    }
}
