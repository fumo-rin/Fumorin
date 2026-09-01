using UnityEngine;
using UnityEngine.EventSystems;

namespace rinCore
{
    public class FactoryFloor : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public void OnPointerDown(PointerEventData eventData)
        {
            new FFac_Floor_Click(eventData).Publish();
            eventData.Use();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            eventData.button = PointerEventData.InputButton.Left;
            OnPointerUp(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            new FFac_Floor_Release(eventData).Publish();
            eventData.Use();
        }
    }
}
