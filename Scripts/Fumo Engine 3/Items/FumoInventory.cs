using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;

namespace rinCore
{
    public partial class ItemTesting
    {
        [System.Serializable]
        public class DebugItemAction : FumoItem.ItemUseAction
        {
            public string text;
            public override void WhenUseSuccess(IFumoItem_Use.unitUsePacket packet)
            {
                Debug.Log(text + " : )");
            }
        }
    }
    public abstract class FumoItem : ScriptableObject
    {
        [System.Serializable]
        public abstract class ItemUseAction
        {
            public abstract void WhenUseSuccess(IFumoItem_Use.unitUsePacket packet);
        }
        [SerializeReference, ManagedReferencePicker] public ItemUseAction UseAction;
        public string ItemID => name;
        public Sprite inventoryIcon;
        public bool Stackable;
        [Range(1, 999), SerializeField] int _stackSize = 250;
        public int MaxStackSize => Stackable ? _stackSize : 1;
        protected void TriggerUseAction(IFumoItem_Use.unitUsePacket packet)
        {
            if (UseAction == null)
                return;
            UseAction.WhenUseSuccess(packet);
        }
    }
    public interface IFumoItem_WeaponItemSwing
    {
        public bool SwapLock => Time.time < SwapLockEnd;
        public bool SwingLock => Time.time < SwingLockEnd;
        public float SwapLockEnd { get; set; }
        public float SwingLockEnd { get; set; }
    }
    public interface IFumoItem_Use
    {
        public struct unitUsePacket
        {
            public FumoUnit Sender;
            public Vector2 Target;
            public FumoSlotItem slotItem;
        }
        public bool TryUseHand(unitUsePacket packet);
    }
    public interface IFumoItem_TileDisplay
    {
        public float Range { get; }
    }

    [System.Serializable]
    public class FumoSlotItem
    {
        public bool IsUseable(out IFumoItem_Use use)
        {
            use = null;
            if (containedItem != null && containedItem is IFumoItem_Use u)
            {
                use = u;
            }
            return use != null;
        }

        public int SlotNumber;
        public FumoItem containedItem;
        public int Amount;
        public float Power;
        public bool ValidItem => containedItem != null;
        public bool ScheduleClear => ValidItem && Amount <= 0;

        public FumoSlotItem() { }

        public FumoSlotItem(int slotNumber)
        {
            this.SlotNumber = slotNumber;
        }

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

        public void Start()
        {
            EventBus.Bind<FInv_AddItem>(AddItem);
            EventBus.Bind<FInv_External_Select_ItemSlot>(ExternalSelectSlot);
        }

        public void End()
        {
            EventBus.Release<FInv_AddItem>(AddItem);
            EventBus.Release<FInv_External_Select_ItemSlot>(ExternalSelectSlot);
        }

        public void AddItem(FInv_AddItem item)
        {
            if (!TryAddItem(item.slotItem))
            {

            }
        }

        public bool SelectSlot(int slot, bool forceRefresh)
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
        private void ExternalSelectSlot(FInv_External_Select_ItemSlot action)
        {
            SelectSlot(action.slot, false);
        }
    }
    #endregion

    #region Event Bus
    public record FInv_External_Select_ItemSlot(int slot, bool forceRefresh);
    public record FInv_AddItem(FumoSlotItem slotItem);
    public record FInv_SelectSlot(int slot, FumoSlotItem containedItem);
    public record FInv_SetSlotItem(int slot, FumoSlotItem newItem);
    public record FInv_SwapSlots(int slot1, int slot2);
    public record FInv_HeldItem_To_UI(FumoSlotItem handItem);
    #endregion

    [System.Serializable]
    public partial class FumoInventory
    {
        public IEnumerable<FInv_SetSlotItem> InventorySnapshot
        {
            get
            {
                EnsureLookupInitialized();
                for (int i = 0; i < slots.Count; i++)
                {
                    var item = slots[i];
                    item.SlotNumber = i;
                    if (item.ValidItem)
                    {
                        RegisterSlotInLookup(item);
                    }
                    yield return new FInv_SetSlotItem(i, item);
                }
            }
        }

        public List<FumoSlotItem> slots = new List<FumoSlotItem>();

        [System.NonSerialized]
        private Dictionary<string, List<FumoSlotItem>> _itemLookup;

        private Dictionary<string, List<FumoSlotItem>> ItemLookup
        {
            get
            {
                EnsureLookupInitialized();
                return _itemLookup;
            }
        }

        private void EnsureLookupInitialized()
        {
            if (_itemLookup == null)
            {
                _itemLookup = new Dictionary<string, List<FumoSlotItem>>();
            }
        }

        public record ItemStackQuery(string itemID, bool notFull);
        public record ItemSlotQuery(int slotIndex);

        public FumoInventory(int maxSlots = 20)
        {
            slots = new List<FumoSlotItem>(maxSlots);
            for (int i = 0; i < maxSlots; i++)
            {
                slots.Add(new FumoSlotItem(i));
            }
            EnsureLookupInitialized();
        }

        public bool TryGetItemStack(ItemStackQuery q, out int slotIndex, out FumoSlotItem item)
        {
            if (ItemLookup.TryGetValue(q.itemID, out List<FumoSlotItem> itemSlots))
            {
                item = itemSlots.FirstOrDefault(s =>
                s.ValidItem && (!q.notFull || s.Amount < s.containedItem.MaxStackSize));

                if (item != null)
                {
                    slotIndex = slots.IndexOf(item);
                    return true;
                }
            }

            slotIndex = -1;
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
                while (item.Amount > 0 && TryGetItemStack(new ItemStackQuery(item.containedItem.ItemID, true), out int slotIdx, out FumoSlotItem existingSlot))
                {
                    int space = item.containedItem.MaxStackSize - existingSlot.Amount;
                    int addAmount = Mathf.Min(space, item.Amount);
                    existingSlot.Amount += addAmount;
                    item.Amount -= addAmount;

                    EventBus.Publish(new FInv_SetSlotItem(slotIdx, existingSlot));
                }
            }

            while (item.Amount > 0)
            {
                int emptyIdx = slots.FindIndex(s => !s.ValidItem);
                if (emptyIdx == -1) return false;

                FumoSlotItem emptySlot = slots[emptyIdx];

                int addAmount = item.containedItem.Stackable ? Mathf.Min(item.containedItem.MaxStackSize, item.Amount) : 1;
                emptySlot.containedItem = item.containedItem;
                emptySlot.Amount = addAmount;
                emptySlot.Power = item.Power;
                item.Amount -= addAmount;

                RegisterSlotInLookup(emptySlot);

                EventBus.Publish(new FInv_SetSlotItem(emptyIdx, emptySlot));
            }

            return true;
        }

        public void CleanEmptySlots()
        {
            for (int i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (slot.ScheduleClear)
                {
                    UnregisterSlotFromLookup(slot);
                    slot.Clear();
                    EventBus.Publish(new FInv_SetSlotItem(i, slot));
                }
            }
        }

        private void RegisterSlotInLookup(FumoSlotItem slot)
        {
            if (!slot.ValidItem) return;

            string id = slot.containedItem.ItemID;
            if (!ItemLookup.TryGetValue(id, out List<FumoSlotItem> itemSlots))
            {
                itemSlots = new List<FumoSlotItem>();
                ItemLookup[id] = itemSlots;
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
            if (ItemLookup.TryGetValue(id, out List<FumoSlotItem> itemSlots))
            {
                itemSlots.Remove(slot);
                if (itemSlots.Count == 0)
                {
                    ItemLookup.Remove(id);
                }
            }
        }
    }
}