using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace rinCore
{
    public record FFac_Floor_Click(PointerEventData data);
    public record FFac_Floor_Release(PointerEventData data);
    public record FFac_SetFactoryPiece(Vector2Int tilePos, FactoryNode item);
    public record FFac_Item_Out(Vector2 position, FumoSlotItem item);

    [System.Serializable]
    public class FactoryNode
    {
        public FumoItemPacket factoryProcessor;
        public FumoItemPacket CurrentItem;
        public string returnedItemID;
        public Vector2Int position;
        public Cardinal direction;
    }
    #region Dummy Testing
    public partial class FactoryRunner
    {
        [SerializeField] private FactoryFloor floorAnchor;
        [SerializeField] Image dummyItem;

        private void StartDummyItem(FFac_Floor_Click action)
        {
            if (action.data.button == PointerEventData.InputButton.Right)
            {
                return;
            }
            Image spawned = Instantiate(dummyItem, floorAnchor.transform, false);

            if (spawned.transform is RectTransform spawnedRect && dummyItem.transform is RectTransform templateRect)
            {
                spawnedRect.anchorMin = templateRect.anchorMin;
                spawnedRect.anchorMax = templateRect.anchorMax;
                spawnedRect.pivot = templateRect.pivot;
                spawnedRect.sizeDelta = templateRect.sizeDelta;
                spawnedRect.localScale = templateRect.localScale;
                spawnedRect.localRotation = templateRect.localRotation;

                RectTransform parentRect = floorAnchor.transform as RectTransform;
                Canvas parentCanvas = floorAnchor.GetComponentInParent<Canvas>();
                Camera uiCamera = (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                    ? parentCanvas.worldCamera
                    : null;

                if (parentRect != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, action.data.position, uiCamera, out Vector2 localPoint))
                {
                    spawnedRect.anchoredPosition = localPoint;
                }
                else
                {
                    spawnedRect.position = action.data.position;
                }
            }
            else
            {
                spawned.transform.position = action.data.position;
            }
        }
    }
    #endregion

    public partial class FactoryRunner : MonoBehaviour
    {
        public static Vector2 ItemOutPosition => CurrentFactoryPosition ?? Vector2.zero;
        public static Vector2? CurrentFactoryPosition = null;
        Dictionary<Vector2Int, FactoryNode> containedItems = new();

        IEnumerable<FactoryNode> AllMachines
        {
            get
            {
                foreach (var item in containedItems.Values)
                {
                    yield return item;
                }
            }
        }

        float nextTickTime;

        void Update()
        {
            if (Time.time > nextTickTime)
            {
                nextTickTime = Time.time + 0.1f;
            }
            TickMachines(0.1f);
        }

        public void TickMachines(float deltaTime)
        {
            foreach (var item in containedItems.Values.ToList())
            {
                if (!containedItems.TryGetValue(item.direction.Int2() + item.position, out FactoryNode nextNode))
                {
                    continue;
                }
                if (item.factoryProcessor is FumoItemPacket p && p.TryAsData(out FumoItem data) && data is FactoryNodeItem nodeItem)
                {
                    nodeItem.ProcessItem(item.CurrentItem, nextNode);
                }
            }
        }

        public void OnEnable()
        {
            nextTickTime = Time.time + 0.5f;
            EventBus.Bind<FFac_SetFactoryPiece>(SetFactoryPiece);
            EventBus.Bind<FFac_Floor_Click>(StartDummyItem);
            EventBus.Bind<BusUI.Close>(CloseUI);
        }

        void OnDisable()
        {
            EventBus.Release<FFac_SetFactoryPiece>(SetFactoryPiece);
            EventBus.Release<FFac_Floor_Click>(StartDummyItem);
            EventBus.Release<BusUI.Close>(CloseUI);
        }
        private void CloseUI(BusUI.Close action)
        {
            if (action.channel.HasAnyOfFlags(BusUI_Queue.AllGame))
                gameObject.SetActive(false);
        }
        public void SetFactoryPiece(FFac_SetFactoryPiece action)
        {
            void ReplaceExisting(FFac_SetFactoryPiece action)
            {
                if (containedItems.TryGetValue(action.tilePos, out FactoryNode existing) && existing.returnedItemID is string existingItemID)
                {
                    if (FumoItem.TryGetFromID(existingItemID, out FumoItem existingItem))
                    {
                        new FFac_Item_Out(ItemOutPosition, new FumoSlotItem()
                        {
                            Amount = 1,
                            containedItem = existingItem,
                            Power = 0,
                            SlotNumber = 0
                        }).Publish();
                    }
                }
            }
            ReplaceExisting(action);
        }
    }
}