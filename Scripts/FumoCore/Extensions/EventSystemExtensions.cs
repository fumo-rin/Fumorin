using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RinCore
{
    public static partial class RinHelper
    {
        public static void EventSystem_Deselect()
        {
            EventSystem_Select(null);
        }
        public static bool EventSystem_Select(GameObject g)
        {
            if (EventSystem.current == null)
            {
                return false;
            }
            EventSystem.current.SetSelectedGameObject(null);
            if (g == null)
            {
                RinHelper.EventSystem_LastSelected = null;
                return false;
            }
            if (g.activeInHierarchy)
            {
                RinHelper.EventSystem_LastSelected = g;
                EventSystem.current.SetSelectedGameObject(g);
                return true;
            }
            return false;
        }
        public static bool HasSelectWithEventSystem
        {
            get
            {
                if (EventSystem.current == null)
                {
                    return false;
                }
                var item = EventSystem.current.currentSelectedGameObject;
                if (item != null)
                {
                    bool itemActive = EventSystem.current.currentSelectedGameObject.activeInHierarchy;
                    return itemActive;
                }
                return false;
            }
        }
        public static GameObject EventSystem_LastSelected;
    }
    public static class EventSystemExtensions
    {
        public static bool Select_WithEventSystem(this GameObject g)
        {
            return RinHelper.EventSystem_Select(g);
        }
    }
}
