using RinCore;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RinCore
{
    [DefaultExecutionOrder(100)]
    public class NavigationElement : MonoBehaviour, IDeselectHandler
    {
        public bool IsLayerDefaultSelection = true;
        [Range(-100, 100)]
        public int weight = -100;
        public bool AutoSelectPerFrame = true;
        private void Start()
        {
            NavigatorUI.BindElement(this);
        }
        private void OnDestroy()
        {
            NavigatorUI.ReleaseElement(this);
        }
        private void OnEnable()
        {
            NavigatorUI.QueueRecalculate();
        }
        public void OnDeselect(BaseEventData eventData)
        {
            if (Helper.EventSystem_LastSelected == gameObject && EventSystem.current.currentSelectedGameObject == null && gameObject != null)
            {
                gameObject.Select_WithEventSystem();
            }
        }
    }
}
