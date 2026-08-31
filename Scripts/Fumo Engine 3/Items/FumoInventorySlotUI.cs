using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace rinCore
{
    [DefaultExecutionOrder(-10)]
    public class FumoInventorySlotUI : MonoBehaviour, IPointerDownHandler
    {
        private static FumoInventorySlotUI unwrapper;

        [SerializeField] private int hotbarCount = 9;
        public int slotIndex;
        private FumoSlotItem containedItem;
        [SerializeField] private Image itemImage;
        [SerializeField] private TMP_Text itemCountText;
        [SerializeField] private Image selectionImage;
        [SerializeField] Slider chargeSlider;
        private Color32 startingColor;

        private void Awake()
        {
            if (unwrapper != null)
            {
                return;
            }

            unwrapper = this;
            Transform parent = transform.parent;

            gameObject.SetActive(false);

            for (int i = 0; i < hotbarCount; i++)
            {
                var c = Instantiate(this, parent);
                c.slotIndex = i;
                if (c.selectionImage != null)
                {
                    c.startingColor = c.selectionImage.color;
                }
                c.gameObject.SetActive(true);
            }
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

        private void LateUpdate()
        {
            if (containedItem == null || !containedItem.ValidItem)
            {
                if (itemImage != null) itemImage.enabled = false;
                if (itemCountText != null) itemCountText.text = "";
                return;
            }

            if (itemImage != null)
            {
                itemImage.sprite = containedItem.containedItem.inventoryIcon;
                itemImage.enabled = true;
            }

            if (itemCountText != null)
            {
                itemCountText.text = containedItem.containedItem.Stackable ? containedItem.Amount.ToShortenedString() : "";
            }
        }

        public void SetItem(FInv_SetSlotItem action)
        {
            if (slotIndex != action.slot)
                return;
            containedItem = action.newItem;
            bool success = false;
            if (success = action.newItem.containedItem is IFumoItem_WeaponItemSwing)
            {
                chargeSlider.SetValuesInt(0, 30, 0, false);
            }
            chargeSlider.gameObject.SetActive(success);
        }

        private void Select(FInv_SelectSlot selection)
        {
            if (selectionImage == null) return;

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