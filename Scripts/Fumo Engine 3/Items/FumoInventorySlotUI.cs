using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace rinCore
{
    [DefaultExecutionOrder(-10)]
    public class FumoInventorySlotUI : MonoBehaviour, IPointerDownHandler
    {
        static FumoInventorySlotUI unwrapper;
        [SerializeField] int hotbarCount = 9;
        int slotIndex;
        FumoSlotItem containedItem;
        [SerializeField] Image itemImage;
        [SerializeField] TMP_Text itemCountText;
        [SerializeField] Image selectionImage;
        Color32 startingColor;
        private void LateUpdate()
        {
            if (containedItem == null || !containedItem.ValidItem)
            {
                itemImage.enabled = false;
                itemCountText.text = "";
                return;
            }
            itemImage.sprite = containedItem.containedItem.inventoryIcon;
            itemCountText.text = containedItem.Amount > 1 ? containedItem.Amount.ToShortenedString() : "";
            itemImage.enabled = true;
        }
        static FumoInventorySlotUI()
        {
            unwrapper = null;
        }
        private void Awake()
        {
            if (unwrapper != null)
            {
                return;
            }
            unwrapper = this;
            Transform parent = transform.parent;
            for (int i = 0; i < hotbarCount; i++)
            {
                var c = Instantiate(this, parent);
                c.slotIndex = i;
                c.startingColor = c.selectionImage.color;
            }
            unwrapper.gameObject.SetActive(false);
        }
        private void OnDestroy()
        {
            if (unwrapper == this)
                unwrapper = null;
        }
        private void OnEnable()
        {
            EventBus.Bind<FInv_SetSlotItem>(SetItem);
            EventBus.Bind<FInv_SelectSlot>(Select);
        }
        private void OnDisable()
        {
            EventBus.Release<FInv_SetSlotItem>(SetItem);
            EventBus.Release<FInv_SelectSlot>(Select);

        }
        public void SetItem(FInv_SetSlotItem action)
        {
            if (slotIndex != action.slot)
                return;
            containedItem = action.newItem;
        }
        void Select(FInv_SelectSlot selection)
        {
            if (selection.slot == slotIndex)
            {
                selectionImage.color = ColorHelper.PastelYellow.Opacity(startingColor.a);
            }
            else
            {
                selectionImage.color = startingColor;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            new FInv_External_Select_ItemSlot(slotIndex, false).Publish();
        }
    }
}
