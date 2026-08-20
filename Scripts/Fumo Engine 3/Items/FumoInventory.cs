using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace rinCore
{
    public abstract class FumoItem : ScriptableObject
    {
        public string ItemID => name;
        public Sprite inventoryIcon;
        public bool Stackable;
        [Range(1, 999), SerializeField] int _stackSize = 250;
        public int MaxStackSize => Stackable ? _stackSize : 1;
    }
    public interface IFumoUseItem
    {
        public struct unitUsePacket
        {
            public FumoUnit Sender;
            public Vector2 Target;
            public FumoSlotItem slotItem;
        }
        public bool TryUse(unitUsePacket packet);
    }
    [System.Serializable]
    public class FumoSlotItem
    {
        public bool IsUseable(out IFumoUseItem use)
        {
            use = null;
            if (containedItem != null && containedItem is IFumoUseItem u)
            {
                use = u;
            }
            return use != null;
        }
        public FumoItem containedItem;
        public int Amount;
        public float Power;
        public bool ValidItem => containedItem != null;
        public bool ScheduleClear => ValidItem && Amount <= 0;

        public FumoSlotItem() { }

        public FumoSlotItem(FumoItem item, int amount = 1, float power = 1.0f)
        {
            containedItem = item;
            Amount = amount;
            Power = power;
        }

        public void Clear()
        {
            containedItem = null;
            Amount = 0;
            Power = 0f;
        }
    }

    #region Actions
    public partial class FumoInventory
    {
        public int CurrentSelectedSlot { get; private set; } = -1;

        public void BindEvents()
        {
            EventBus.Bind<FInv_AddItem>(AddItem);
        }

        public void ReleaseEvents()
        {
            EventBus.Release<FInv_AddItem>(AddItem);
        }

        public void AddItem(FInv_AddItem item)
        {
            if (!TryAddItem(item.slotItem))
            {

            }
        }

        public bool SelectSlot(int slot)
        {
            if (CurrentSelectedSlot == slot)
            {
                return false;
            }

            FumoSlotItem selectedItem = null;
            if (TryGetItemSlot(new ItemSlotQuery(slot), out FumoSlotItem item))
            {
                selectedItem = item;
            }

            EventBus.Publish(new FInv_SelectSlot(slot, selectedItem));
            CurrentSelectedSlot = slot;
            return true;
        }
    }
    #endregion

    #region Event Bus
    public record FInv_AddItem(FumoSlotItem slotItem);
    public record FInv_SelectSlot(int slot, FumoSlotItem containedItem);
    #endregion

    [System.Serializable]
    public partial class FumoInventory
    {
        public List<FumoSlotItem> slots = new List<FumoSlotItem>();
        private readonly Dictionary<string, List<FumoSlotItem>> _itemLookup = new Dictionary<string, List<FumoSlotItem>>();

        public record ItemStackQuery(string itemID, bool notFull);
        public record ItemSlotQuery(int slotIndex);

        public FumoInventory(int maxSlots = 20)
        {
            slots = new List<FumoSlotItem>(maxSlots);
            for (int i = 0; i < maxSlots; i++)
            {
                slots.Add(new FumoSlotItem());
            }
        }

        public bool TryGetItemStack(ItemStackQuery q, out FumoSlotItem item)
        {
            if (_itemLookup.TryGetValue(q.itemID, out List<FumoSlotItem> itemSlots))
            {
                item = itemSlots.FirstOrDefault(s =>
                    s.ValidItem &&
                    (!q.notFull || s.Amount < s.containedItem.MaxStackSize)
                );
                return item != null;
            }

            item = null;
            return false;
        }

        public bool TryGetItemSlot(ItemSlotQuery q, out FumoSlotItem item)
        {
            if (q.slotIndex >= 0 && q.slotIndex < slots.Count)
            {
                item = slots[q.slotIndex];
                return true;
            }
            item = null;
            return false;
        }

        public bool TryAddItem(FumoSlotItem item)
        {
            if (item == null || item.containedItem == null || item.Amount <= 0) return false;

            if (item.containedItem.Stackable)
            {
                while (item.Amount > 0 && TryGetItemStack(new ItemStackQuery(item.containedItem.ItemID, true), out FumoSlotItem existingSlot))
                {
                    int space = item.containedItem.MaxStackSize - existingSlot.Amount;
                    int addAmount = Mathf.Min(space, item.Amount);
                    existingSlot.Amount += addAmount;
                    item.Amount -= addAmount;
                }
            }

            while (item.Amount > 0)
            {
                FumoSlotItem emptySlot = slots.FirstOrDefault(s => !s.ValidItem);
                if (emptySlot == null) return false;

                int addAmount = item.containedItem.Stackable ? Mathf.Min(item.containedItem.MaxStackSize, item.Amount) : 1;
                emptySlot.containedItem = item.containedItem;
                emptySlot.Amount = addAmount;
                emptySlot.Power = item.Power;
                item.Amount -= addAmount;

                RegisterSlotInLookup(emptySlot);
            }

            return true;
        }

        public void CleanEmptySlots()
        {
            foreach (var slot in slots.Where(x => x.ScheduleClear))
            {
                UnregisterSlotFromLookup(slot);
                slot.Clear();
            }
        }

        private void RegisterSlotInLookup(FumoSlotItem slot)
        {
            if (!slot.ValidItem) return;

            string id = slot.containedItem.ItemID;
            if (!_itemLookup.TryGetValue(id, out List<FumoSlotItem> itemSlots))
            {
                itemSlots = new List<FumoSlotItem>();
                _itemLookup[id] = itemSlots;
            }

            if (!itemSlots.Contains(slot))
            {
                itemSlots.Add(slot);
            }
        }

        private void UnregisterSlotFromLookup(FumoSlotItem slot)
        {
            if (!slot.ValidItem) return;

            string id = slot.containedItem.ItemID;
            if (_itemLookup.TryGetValue(id, out List<FumoSlotItem> itemSlots))
            {
                itemSlots.Remove(slot);
                if (itemSlots.Count == 0)
                {
                    _itemLookup.Remove(id);
                }
            }
        }
    }
}