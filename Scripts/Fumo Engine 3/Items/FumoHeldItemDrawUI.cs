using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace rinCore
{
    public class FumoHeldItemDrawUI : MonoBehaviour, IHierarchyComponentColor
    {
        [SerializeField] Image OptionalImage;
        [SerializeField] TMP_Text ItemNameText, AmountText;

        public Color LabelColor => ColorHelper.PastelBlue.Opacity(50);

        private void Awake()
        {
            EventBus.Bind<FInv_HeldItem_To_UI>(Apply);
        }
        private void OnDestroy()
        {
            EventBus.Release<FInv_HeldItem_To_UI>(Apply);
        }
        private void Apply(FInv_HeldItem_To_UI action)
        {
            bool validItem = action.handItem.ValidItem;
            if (!validItem)
            {
                if (OptionalImage != null)
                {
                    OptionalImage = null;
                    OptionalImage.color = ColorHelper.White.Opacity(0);
                }
                if (ItemNameText != null)
                    ItemNameText.text = "";
                if (AmountText != null)
                    AmountText.text = "";
                return;
            }
            var item = action.handItem.containedItem;
            if (OptionalImage != null)
            {
                OptionalImage.color = ColorHelper.White.Opacity(255);
                OptionalImage.sprite = item.inventoryIcon;
            }
            if (ItemNameText != null)
            {
                ItemNameText.text = item.name.RemoveAfter("#").StripUnityNameSuffix().Capitalized().SpaceByCapitals();
            }
            string text = "";//item rarity symbol?
            if (AmountText != null)
            {
                if (/*action.handItem.UsesCharges*/ false)
                {
                    //text += $"{}";
                    AmountText.text = text;
                    return;
                }
                if (action.handItem.Amount >= 1 && item.Stackable)
                {
                    text += $"x{action.handItem.Amount}";
                    AmountText.text = text;
                    return;
                }
                AmountText.text = "";
            }
        }
    }
}
