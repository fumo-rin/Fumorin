using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using RinCore;
using UnityEngine.EventSystems;

namespace RinCore
{
    public class NavigatorUI : MonoBehaviour
    {
        static List<NavigationElement> activeNavigation;
        [SerializeField] InputActionReference UIMoveInput;
        static bool recalculate;
        [Initialize(-50)]
        private static void ReinitializeNavigatorUI()
        {
            activeNavigation = new List<NavigationElement>();
            recalculate = true;
        }
        private void Awake()
        {
            if (activeNavigation != null)
            {
                Destroy(this);
            }
            ReinitializeNavigatorUI();
        }
        bool FindDefaultSelection(out NavigationElement defaultSelection, bool isFrame = true)
        {
            defaultSelection = null;
            activeNavigation.Sort((NavigationElement a, NavigationElement b) => a.weight.CompareTo(b.weight));
            int highestWeight = -99999;
            NavigationElement iteration = null;
            for (int i = 0; i < activeNavigation.Count; i++)
            {
                iteration = activeNavigation[i];
                if (iteration == null)
                {
                    activeNavigation.RemoveAt(i);
                    i--;
                    continue;
                }
                if (!iteration.AutoSelectPerFrame)
                {
                    continue;
                }
                if (iteration == null || !iteration.gameObject.activeInHierarchy)
                {
                    continue;
                }
                if (iteration.weight >= highestWeight)
                {
                    defaultSelection = iteration;
                    highestWeight = iteration.weight;
                }
            }
            return defaultSelection != null;
        }
        public static void QueueRecalculate()
        {
            recalculate = true;
        }
        private static bool RunRecalculate()
        {
            if (recalculate)
            {
                recalculate = false;
                return true;
            }
            return false;
        }
        public static void BindElement(NavigationElement e)
        {
            activeNavigation.Add(e);
            recalculate = true;
        }
        public static void ReleaseElement(NavigationElement e)
        {
            activeNavigation.Remove(e);
            recalculate = true;
        }
        void LateUpdate()
        {
            if (EventSystem.current == null)
            {
                return;
            }
            NavigationElement n = null;
            if (RunRecalculate())
            {
                FindDefaultSelection(out n);
                if (n != null)
                {
                    n.gameObject.Select_WithEventSystem();
                }
                return;
            }
            FindDefaultSelection(out n);
            if (EventSystem.current.currentSelectedGameObject is GameObject g && g != null)
            {
                if (!g.activeInHierarchy && n != null)
                {
                    n.gameObject.Select_WithEventSystem();
                }
            }
            if ((EventSystem.current.currentSelectedGameObject == null) && n != null)
            {
                n.gameObject.Select_WithEventSystem();
            }
        }
    }
}
