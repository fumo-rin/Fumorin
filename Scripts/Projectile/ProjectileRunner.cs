using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.SceneManagement;
using rinCore;

namespace rinCore.Bullet
{
    #region Hit Particle
    public partial class ProjectileRenderer
    {
        [SerializeField] private ParticleSystem hitParticle;

        public struct HitParticleSettings
        {
            public Color32? colorOverride;
            public float forceMultiplier;
        }

        static readonly HitParticleSettings defaultSetting = new()
        {
            colorOverride = null,
            forceMultiplier = 1f
        };

        public static void HitParticle(Vector2 position, Vector2 normal, HitParticleSettings? setting = null)
        {
            if (instance != null && instance.hitParticle != null)
            {
                float force = setting == null ? defaultSetting.forceMultiplier : setting.Value.forceMultiplier;
                Color32? color = setting == null ? defaultSetting.colorOverride : setting.Value.colorOverride;

                Quaternion rotation = Quaternion.FromToRotation(Vector3.right, normal);

                instance.hitParticle.PlayOneShotCached(
                    position: position,
                    rotation: rotation,
                    overrideBurstCount: 6,
                    colorOverride: color,
                    sizeMultiplier: force
                );
            }
        }
    }
    #endregion

    #region Bullet Cancel
    public partial class ProjectileRenderer
    {
        [SerializeField] private ParticleSystem bulletCancelParticlePrefab;

        public static void BulletCancelParticle(Vector3 position, Vector3? velocity = null, float velocityMultiplier = 0.4f)
        {
            if (instance == null || instance.bulletCancelParticlePrefab == null)
                return;
            instance.bulletCancelParticlePrefab.EmitSingleParticleCached(position, velocity * velocityMultiplier, 50f);
        }
    }
    #endregion

    #region Bullet Flare
    public partial class ProjectileRenderer
    {
        [SerializeField] private ParticleSystem bulletFlareParticlePrefab;

        public static void BulletFlareParticle(Vector3 position, Color32 color, Vector3? velocity = null, float sizeMultiplier = 1f)
        {
            if (instance == null || instance.bulletFlareParticlePrefab == null)
                return;

            instance.bulletFlareParticlePrefab.EmitSingleParticleCached(
                position + new Vector3(0, 0, -1),
                velocity ?? Vector3.zero,
                0f,
                color,
                sizeMultiplier
            );
        }
    }
    #endregion

    #region Optimized High-Performance Renderer
    [DefaultExecutionOrder(-500)]
    [System.Serializable]
    public partial class ProjectileRenderer
    {
        private static ProjectileRenderer instance;

        private NativeList<ParticleSystem.Particle> particleBuffer;
        private ParticleSystem.Particle[] managedCacheArray;
        private Dictionary<int, ParticleSystem> systemDictByID;
        private Dictionary<int, List<Projectile>> fastDefineLookup;
        private List<ProjectileDefine> activeDefinesList;

        const float growTime = 0.075f;
        const float shrinkTime = 0.1f;
        const float peakScale = 2f;

        public void Bind()
        {
            instance = this;
            systemDictByID = new Dictionary<int, ParticleSystem>(32);
            fastDefineLookup = new Dictionary<int, List<Projectile>>(32);
            activeDefinesList = new List<ProjectileDefine>(32);
            particleBuffer = new NativeList<ParticleSystem.Particle>(100000, Allocator.Persistent);
            managedCacheArray = new ParticleSystem.Particle[100000];
        }

        public void Release()
        {
            if (instance == this) instance = null;

            if (systemDictByID != null)
            {
                foreach (var ps in systemDictByID.Values)
                {
                    if (ps != null) MonoBehaviour.Destroy(ps.gameObject);
                }
                systemDictByID.Clear();
            }

            if (particleBuffer.IsCreated) particleBuffer.Dispose();
            fastDefineLookup?.Clear();
            activeDefinesList?.Clear();
        }

        public static void AddDefine(ProjectileDefine d)
        {
            if (instance == null || d == null) return;
            instance.CreateParticleSystemForDefine(d);
        }
        private void CreateParticleSystemForDefine(ProjectileDefine define)
        {
            int key = define.GetInstanceID();
            if (systemDictByID.ContainsKey(key)) return;

            try
            {
                if (!define.StartPS(out ParticleSystem psInstance) || psInstance == null) return;

                psInstance.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = psInstance.main;
                main.maxParticles = 100000;

                var psRenderer = psInstance.GetComponent<ParticleSystemRenderer>();
                if (psRenderer != null)
                {
                    psRenderer.alignment = ParticleSystemRenderSpace.View;
                    psRenderer.sortMode = ParticleSystemSortMode.OldestInFront;
                    psRenderer.sortingLayerName = define.SortingLayer;
                    psRenderer.sortingOrder = define.sortingLayer;
                }

                systemDictByID[key] = psInstance;
                fastDefineLookup[key] = new List<Projectile>(8192);
                activeDefinesList.Add(define);
            }
            catch (Exception ex)
            {
                Debug.LogError($"ProjectileSystem: Exception for define '{define.name}': {ex}");
            }
        }

        public void RenderProjectileFrameFast(List<Projectile> allProjectiles, float currentTime)
        {
            if (systemDictByID == null || allProjectiles == null) return;

            foreach (var list in fastDefineLookup.Values)
            {
                list.Clear();
            }

            int projCount = allProjectiles.Count;
            for (int i = 0; i < projCount; i++)
            {
                var p = allProjectiles[i];
                if (p == null || !p.IsValid || p.data == null) continue;

                int key = p.data.GetInstanceID();
                if (!fastDefineLookup.TryGetValue(key, out var list))
                {
                    CreateParticleSystemForDefine(p.data);
                    if (!fastDefineLookup.TryGetValue(key, out list)) continue;
                }
                list.Add(p);
            }

            for (int i = 0; i < activeDefinesList.Count; i++)
            {
                var define = activeDefinesList[i];
                int key = define.GetInstanceID();

                if (!fastDefineLookup.TryGetValue(key, out var batch) || batch.Count == 0)
                {
                    if (systemDictByID.TryGetValue(key, out var psEmpty)) psEmpty.Clear();
                    continue;
                }

                if (!systemDictByID.TryGetValue(key, out var ps)) continue;

                var renderer = ps.GetComponent<ParticleSystemRenderer>();
                if (renderer != null)
                {
                    if (renderer.sortingLayerName != define.SortingLayer)
                        renderer.sortingLayerName = define.SortingLayer;
                    renderer.sortingOrder = define.sortingLayer;
                }

                int count = batch.Count;
                if (managedCacheArray.Length < count)
                {
                    managedCacheArray = new ParticleSystem.Particle[Mathf.NextPowerOfTwo(count)];
                }

                var textureSheetAnimation = ps.textureSheetAnimation;
                int totalFrames = textureSheetAnimation.numTilesX * textureSheetAnimation.numTilesY;
                float animSpeed = define.animationSpeed <= 0.01f ? 1f : totalFrames / define.animationSpeed;

                Color32 particleColor = define.ProjectileTint;

                for (int j = 0; j < count; j++)
                {
                    var p = batch[j];
                    float animElapsed = currentTime - (p.spawnTime + p.animationOffsetSeconds);
                    float unmodifiedElapsed = currentTime - p.spawnTime;
                    float addedSpin = animElapsed * define.spin;

                    float currentRemaining = Mathf.Max(0.001f, animSpeed - (animElapsed % animSpeed));

                    float scaleFactor = 1f;
                    if (define.Flare)
                    {
                        if (unmodifiedElapsed <= growTime)
                            scaleFactor = Mathf.SmoothStep(0f, peakScale, unmodifiedElapsed / growTime);
                        else if (unmodifiedElapsed <= growTime + shrinkTime)
                            scaleFactor = Mathf.Lerp(peakScale, 1f, (unmodifiedElapsed - growTime) / shrinkTime);
                    }
                    else
                        if (unmodifiedElapsed <= growTime)
                            scaleFactor = Mathf.SmoothStep(0.35f, 1f, unmodifiedElapsed / growTime);

                    float angle = define.LockRotation ? 0f : p.Render_Angle + addedSpin - 90f;

                    managedCacheArray[j] = new ParticleSystem.Particle
                    {
                        position = new Vector3(p.FinalizedPosition.x, p.FinalizedPosition.y, 0f),
                        startSize = define.Size * scaleFactor,
                        startLifetime = animSpeed,
                        remainingLifetime = currentRemaining,
                        startColor = particleColor,
                        rotation = angle
                    };
                }

                ps.SetParticles(managedCacheArray, count);
            }
        }
    }
    #endregion

    #region Slowdown Calculation
    public partial class ProjectileRunner
    {
        public static int? SlowdownProjectileTargetCount = null;

        public static float GetTargetSlowdown(int requiredProjectiles = 400)
        {
            if (!FumoUnit.PlayerAs<FumoUnit>(out FumoUnit player) || !player.IsAlive) return 1f;
            if (player is IUnitIframes iframes && iframes.IFramesRemaining > 0.8f) return 1f;

            float slowdownIdeal = 0.666f;
            float slowdownMax = 0.45f;
            float slowdownNone = 1f;

            float overloadStart = requiredProjectiles * 2f;
            float overloadEnd = requiredProjectiles * 4f;
            int halfRequired = (int)(requiredProjectiles * 0.5f);

            int bulletCount = BulletCount;

            if (bulletCount <= halfRequired) return slowdownNone;
            if (bulletCount <= requiredProjectiles)
            {
                float t = (bulletCount - halfRequired) / (float)(requiredProjectiles - halfRequired);
                return Mathf.Lerp(slowdownNone, slowdownIdeal, t);
            }
            if (bulletCount <= overloadStart) return slowdownIdeal;

            float overloadT = Mathf.InverseLerp(overloadStart, overloadEnd, bulletCount);
            return Mathf.Lerp(slowdownIdeal, slowdownMax, overloadT);
        }
    }
    #endregion

    #region Main Runner Implementation
    public partial class ProjectileRunner : MonoBehaviour
    {
        [SerializeField] LayerMask CollisionLayer;
        [SerializeField] ProjectileRenderer _renderer = new();
        private List<Projectile> masterProjectileList = new(100000);
        private static ProjectileRunner current;

        public static void InjectProjectile(Projectile p)
        {
            if (current == null || p == null || p.data == null) return;
            ProjectileRenderer.AddDefine(p.data);
            current.masterProjectileList.Add(p);
            if (p.data.Flare)
            {
                ProjectileRenderer.BulletFlareParticle(p.FinalizedPosition, p.data.FlareColor, p.FinalizedVelocity, 2.35f);
            }
        }

        private void Awake()
        {
            current = this;
            masterProjectileList = new List<Projectile>(100000);
        }

        public static int BulletCount => current != null ? current.masterProjectileList.Count : 0;

        private void OnEnable() => _renderer.Bind();
        private void OnDisable() => _renderer.Release();

        private void Update()
        {
            int count = masterProjectileList.Count;
            if (count == 0) return;

            float dt = Time.deltaTime;
            Projectile.ProcessBatch(masterProjectileList, dt, CollisionLayer, (Projectile.IProjectileHit hit) =>
            {
                hit.HitTransform.position += new Vector3(0, -1f, 0f);
            });

            masterProjectileList.RemoveAll(x => x == null || !x.IsValid);

            TimeSlowHandler.SetSimulatedSlowdownTarget(GetTargetSlowdown(SlowdownProjectileTargetCount ?? 400));
        }

        private void LateUpdate()
        {
            Render();
            new FEB_Projectile_Count_Frame(BulletCount).Publish();
        }

        private void Render()
        {
            _renderer.RenderProjectileFrameFast(masterProjectileList, Time.time);
        }
    }

    public record FEB_Projectile_Count_Frame(int ProjectileCount);
    #endregion
}