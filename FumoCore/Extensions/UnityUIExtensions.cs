using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RinCore
{
    public static class UnityUIExtensions
    {
        public static void BindSingleAction(this Button b, Action call)
        {
            b.onClick.RemoveAllListeners();
            b.onClick.AddListener(() => call?.Invoke());
        }
        public static void AddClickAction(this Button b, Action c)
        {
            b.onClick.AddListener(() => c?.Invoke());
        }
        public static void RemoveClickAction(this Button b, Action c)
        {
            b.onClick.RemoveListener(() => c?.Invoke());
        }
        public static void RemoveAllClickActions(this Button b)
        {
            b.onClick.RemoveAllListeners();
        }
        public static void BindSingleEventAction(this Button button, UnityAction action, params EventTriggerType[] eventTypes)
        {
            if (button == null) return;

            if (eventTypes == null || eventTypes.Length == 0)
            {
                eventTypes = new[] { EventTriggerType.Submit, EventTriggerType.PointerClick };
                //eventTypes = (EventTriggerType[])Enum.GetValues(typeof(EventTriggerType));
            }

            EventTrigger trigger = button.GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = button.gameObject.AddComponent<EventTrigger>();

            trigger.triggers.RemoveAll(entry => eventTypes.Contains(entry.eventID));

            foreach (var eventType in eventTypes)
            {
                EventTrigger.Entry entry = new EventTrigger.Entry
                {
                    eventID = eventType
                };
                entry.callback.AddListener(_ => action?.Invoke());
                trigger.triggers.Add(entry);
            }
        }
    }
}
