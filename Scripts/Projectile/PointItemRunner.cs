using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace rinCore
{
    #region loot effect
    public partial class PointItemRunner
    {
        public record FEB_External_LootEffect(List<Vector2> points, Vector2 fallbackEndPosition);
        List<LootEffectItem> currentEffects = new(1 << 10);
        [SerializeField] ParticleSystem lootDrawer;

        struct LootEffectItem : IParticlePosition
        {
            [Initialize(0)]
            private static void clear()
            {
                playerOverride = null;
            }
            public static Vector2? playerOverride;
            public float creationTime;
            public float lerp01 => Time.time.MapTo01(creationTime, creationTime + 0.35f, true);
            public Vector2 start, fallbackEnd;
            public Vector3 ParticlePosition => start.LerpUnclamped(playerOverride ?? fallbackEnd, LerpCurves.EaseInSine(lerp01.Clamp(0f, 1f)));
        }
        private void ExternalLootEffectInjection(FEB_External_LootEffect injection)
        {
            for (int i = 0; i < injection.points.Count; i++)
            {
                currentEffects.Add(new LootEffectItem
                {
                    creationTime = Time.time + RNG.FloatRange(0.02f, 0.1f),
                    fallbackEnd = injection.fallbackEndPosition,
                    start = injection.points[i],
                });
            }
        }
        void BatchedLootFrame(List<LootEffectItem> items, Vector2? playerPosition = null)
        {
            LootEffectItem.playerOverride = playerPosition;
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (item.lerp01 >= 1f)
                {
                    items.RemoveAndReplaceWithLast(i);
                    i--;
                    continue;
                }
                items[i] = item;
            }
            lootDrawer.FC_RenderAnimatedPointsFrame(currentEffects, 1f, true);
        }
    }
    #endregion

    public partial class PointItemRunner : MonoBehaviour
    {
        public record FEB_Create(List<Vector2> points, CreationPacket creation);
        public struct CreationPacket
        {
            public int itemValue;
            public byte renderIndex;
            public float maxUpAngleArc;
            public Vector2 forceRange;
            public Vector2 RandomVelocity => Vector2.up
                .Rotate2D(RNG.FloatRange(-maxUpAngleArc.Half(), maxUpAngleArc.Half()))
                .ScaleToMagnitude(forceRange.RandomBetweenXY());
        }

        struct PointItem : IParticlePosition
        {
            public Vector2 position;
            public Vector2 currentVelocity;
            public float spawnTime;
            public int itemValue;
            public byte renderIndex;
            public bool isPickedUp;
            public Vector3 ParticlePosition => position;
        }

        [SerializeField] InputActionReference focusAction;
        float lastFocusPress;
        [SerializeField] List<ParticleSystem> particleRenderers;
        Dictionary<byte, List<PointItem>> activeItems = new();
        [SerializeField] Vector2 Gravity = new(0f, -4.25f);
        FumoUnit trackedPlayer => FumoUnit.Player;

        void OnEnable()
        {
            EventBus.Bind<FEB_Create>(StartFromEvent);
        }

        void OnDisable()
        {
            EventBus.Release<FEB_Create>(StartFromEvent);
        }

        private void StartFromEvent(FEB_Create packet)
        {
            StartItems(packet.creation, packet.points);
        }

        private void StartItems(CreationPacket packet, List<Vector2> points)
        {
            if (!activeItems.TryGetValue(packet.renderIndex, out List<PointItem> current))
            {
                current = new List<PointItem>(points.Count);
                activeItems.Add(packet.renderIndex, current);
            }

            float currentTime = Time.time;
            int count = points.Count;
            for (int i = 0; i < count; i++)
            {
                current.Add(new PointItem
                {
                    position = points[i],
                    currentVelocity = packet.RandomVelocity,
                    spawnTime = currentTime,
                    itemValue = packet.itemValue,
                    renderIndex = packet.renderIndex,
                    isPickedUp = false
                });
            }
        }

        void Update()
        {
            float currentTime = Time.time;
            float deltaTime = Time.deltaTime;

            if (focusAction.IsPressedRaw())
            {
                lastFocusPress = currentTime;
            }

            const float MAX_FOCUS_EFFECT_TIME = 0.75f, MIN_PICKUP = 2.5f, MAX_PICKUP = 10f;
            float pickupRadius = (currentTime - lastFocusPress).MapTo01(0f, MAX_FOCUS_EFFECT_TIME).MapFrom01(MIN_PICKUP, MAX_PICKUP);

            bool playerExists = trackedPlayer != null && trackedPlayer.IsAlive;
            Vector2? playerPosition = null;
            Vector2 playerPosVal = default;

            if (playerExists)
            {
                playerPosition = trackedPlayer.CenterOrCurrentPosition;
                playerPosVal = playerPosition.Value;
            }

            int rendererCount = particleRenderers.Count;

            foreach (var kvp in activeItems)
            {
                var collection = kvp.Value;
                byte key = kvp.Key;

                for (int i = 0; i < collection.Count; i++)
                {
                    PointItem iteration = collection[i];

                    if (iteration.isPickedUp)
                    {
                        collection.RemoveAndReplaceWithLast(i);
                        i--;
                        continue;
                    }

                    iteration.currentVelocity = iteration.currentVelocity.LerpTowards(Gravity, 3f);
                    iteration.position += iteration.currentVelocity * deltaTime;

                    bool canPickup = currentTime >= iteration.spawnTime + 0.45f;

                    if (canPickup && playerPosition.HasValue && iteration.position.SquareDistanceToLessThan(playerPosVal, pickupRadius))
                    {
                        iteration.isPickedUp = true;
                        currentEffects.Add(new LootEffectItem
                        {
                            creationTime = currentTime + RNG.FloatRange(0.02f, 0.1f),
                            fallbackEnd = playerPosVal,
                            start = iteration.position,
                        });
                    }

                    collection[i] = iteration;
                }

                if (rendererCount > 0)
                {
                    ParticleSystem ps = particleRenderers[key % rendererCount];
                    ps.FC_RenderAnimatedPointsFrame(collection, 1f);
                }
            }

            this.BatchedLootFrame(currentEffects, playerPosition);
        }
    }
}