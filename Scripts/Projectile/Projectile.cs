using System;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using System.Linq;

namespace rinCore.Bullet
{
    public interface IParticleRenderItem
    {
        public bool SkipRender { get; }
        public Vector2 Render_Position { get; }
        public float Render_Angle { get; }
        public float Render_Size { get; }
    }
    public class Projectile : IParticleRenderItem
    {
        public interface IProjectileHitListener
        {

        }
        [NonSerialized] public FumoUnit Sender;
        [NonSerialized] public ProjectileDefine data;
        [HideInInspector] public float spawnTime;
        [HideInInspector] public float animationOffsetSeconds;
        [HideInInspector] public bool IsValid { get; set; }
        [HideInInspector] public Vector2 Render_Position => FinalizedPosition;
        [HideInInspector] public float Render_Angle => FastAtan2Deg(_regularVelocity.x, _regularVelocity.y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float FastAtan2Deg(float y, float x)
        {
            float angle = math.atan2(y, x) * Mathf.Rad2Deg;
            return angle < 0f ? angle + 360f : angle;
        }
        [HideInInspector] public float Render_Size => 1f;
        [HideInInspector] Vector2 _currentPosition;
        public bool SkipRender => !IsValid;
        public Vector2 FinalizedPosition
        {
            get
            {
                return _currentPosition;
            }
        }
        Vector2 _regularVelocity;
        public Vector2 ExtraVelocity;
        Vector2? PreviousPosition = null;
        public bool HasPreviousPosition(out Vector2 pos)
        {
            pos = PreviousPosition ?? default;
            return pos != default;
        }
        public Vector2 FinalizedVelocity
        {
            get
            {
                return _regularVelocity + ExtraVelocity;
            }
        }
        public interface IProjectileHit
        {
            public Transform HitTransform => (this is Component comp) ? comp.transform : null;
        }
        static RaycastHit2D[] hits = new RaycastHit2D[10];
        static HashSet<IProjectileHit> hitList = new();
        static ContactFilter2D batchContactFilter = new ContactFilter2D()
        {
            useLayerMask = true,
            useTriggers = false
        };
        public static void ProcessBatch(IEnumerable<Projectile> projCollection, float dt, LayerMask hitLayers, Action<IProjectileHit> hitAction)
        {
            batchContactFilter.SetLayerMask(hitLayers);

            foreach (var proj in projCollection)
            {
                if (!proj.IsValid)
                    continue;

                Vector2 startPos = proj.FinalizedPosition;
                proj.PreviousPosition = startPos;
                Vector2 moveDelta = dt * proj.FinalizedVelocity;
                Vector2 endPos = startPos + moveDelta;
                proj._currentPosition = endPos;
                float travelDistance = moveDelta.magnitude;
                Vector2 castDirection = travelDistance > 0.0001f ? moveDelta / travelDistance : Vector2.zero;

                int hitsCount = Physics2D.CircleCast(startPos, proj.data.CollisionRadius, castDirection, batchContactFilter, hits, travelDistance);
                if (hitsCount > 0)
                {
                    hitList.Clear();
                    for (int i = 0; i < hitsCount; i++)
                    {
                        RaycastHit2D hit = hits[i];
                        Transform hitTrans = hit.transform;

                        if (hitTrans == null)
                            continue;

                        if (!hitTrans.TryGetComponent(out IProjectileHit ihit))
                        {
                            proj.IsValid = false;

                            ProjectileRenderer.HitParticle(hit.point - hit.normal.ScaleToMagnitude(.25f), hit.normal, new()
                            {
                                colorOverride = null,
                                forceMultiplier = 1f
                            });
                            continue;
                        }

                        if (proj.Sender == (object)ihit || !hitList.Add(ihit))
                            continue;


                        ProjectileRenderer.HitParticle(hit.point, hit.normal, new()
                        {
                            colorOverride = null,
                            forceMultiplier = 1f
                        });

                        proj.IsValid = false;
                        hitAction?.Invoke(ihit);
                    }
                }
            }
        }
        public struct BulletPacket
        {
            public ProjectileDefine Define;
            public FumoUnit Sender;
            public Vector2 Position;
            public Vector2 VelocityDirection;
        }
        public static Projectile BuildProjectile(BulletPacket b)
        {
            if (CreateProjectile(b.Define, b.Sender, b.Position, b.VelocityDirection, out Projectile newP))
            {
                return newP;
            }
            return null;
        }
        static bool CreateProjectile(ProjectileDefine define, FumoUnit sender, Vector2 position, Vector2 velocityDirection, out Projectile p)
        {
            void Cancel(Vector2 position, Vector2 direction)
            {
                ProjectileRenderer.BulletCancelParticle(position, direction);
            }
            p = default;
            if (define == null)
            {
                return false;
            }
            bool SweepThisBullet = false;
            if (SweepThisBullet)
            {
                bool RNG = false;
                //RNG = ProjectileRunner.SweepLootChance > 0 && RNG.Byte255 < ProjectileRunner.SweepLootChance;
                if (RNG)
                {
                    //PointItemRunner.SpawnPointItem(position + Random.insideUnitCircle);
                    Cancel(position, velocityDirection);
                }
                return false;
            }
            p = new Projectile
            {
                data = define,
                _currentPosition = position,
                PreviousPosition = position,
                _regularVelocity = velocityDirection,
                spawnTime = Time.time,
                animationOffsetSeconds = (1f / define.animationSpeed) * (define.animationSpreadPercent.RandomPositiveNegativeRange().Multiply(0.01f)),
                //mods = mods?.Select(m => m.Clone()).ToList(),
                IsValid = true
            };
            p.Sender = sender;
            ProjectileRunner.InjectProjectile(p);
            return true;
        }
    }
}
