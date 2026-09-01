using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace rinCore
{
    public partial struct BusUI
    {
        public record Close(BusUI_Queue channel);
    }

    [Flags]
    public enum BusUI_Queue
    {
        None = 0,
        QueuePop = 1 << 0,
        AllGame = 1 << 1,
        UIOverlays = 1 << 2,
        All = ~0
    }

    public class BusUI_Close : MonoBehaviour, IPointerDownHandler
    {
        [SerializeField] private BusUI_Queue channel = BusUI_Queue.All;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                eventData.Use();
                new BusUI.Close(channel).Publish();
            }
        }
    }
}