using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace rinCore
{
    public struct FEB_EventSystem_SelectBuffered : IRinEvent
    {
        public GameObject queuedSelection;
        public FEB_EventSystem_SelectBuffered(GameObject g)
        {
            queuedSelection = g;
        }
    }

    public static partial class RinHelper
    {
        public static void EventSystem_Deselect()
        {
            EventSystem_Select(null);
        }

        private static readonly Stack<FEB_EventSystem_SelectBuffered> selectionStack = new Stack<FEB_EventSystem_SelectBuffered>();
        private static SelectionBufferRunner runnerInstance;

        private class SelectionBufferRunner : MonoBehaviour
        {
            private void LateUpdate()
            {
                if (SceneLoader.IsLoading) return;

                if (EventSystem.current != null && selectionStack.Count > 0)
                {
                    FEB_EventSystem_SelectBuffered lastSubmitted = selectionStack.Pop();

                    if (lastSubmitted.queuedSelection != null && lastSubmitted.queuedSelection.activeInHierarchy)
                    {
                        Debug.Log("Executing Last Buffered Selection: " + lastSubmitted.queuedSelection.name);
                        lastSubmitted.queuedSelection.Select_WithEventSystem();
                    }
                    else if (lastSubmitted.queuedSelection == null)
                    {
                        EventSystem_Deselect();
                    }
                }
                selectionStack.Clear();
                runnerInstance = null;
                Destroy(gameObject);
            }
        }

        [Initialize(191919)]
        private static void BindSelectionBuffer()
        {
            EventBus.Clear<FEB_EventSystem_SelectBuffered>();
            selectionStack.Clear();

            EventBus.Bind<FEB_EventSystem_SelectBuffered>((a) =>
            {
                if (a.queuedSelection != null)
                {
                    Debug.Log("Pushing queued selection to stack: " + a.queuedSelection.name);
                }
                else
                {
                    Debug.Log("Pushing clear selection request to stack.");
                }

                selectionStack.Push(a);

                if (runnerInstance == null)
                {
                    GameObject runnerGO = new GameObject("[SelectionBufferRunner]");
                    Object.DontDestroyOnLoad(runnerGO);
                    runnerInstance = runnerGO.AddComponent<SelectionBufferRunner>();
                }
            });
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