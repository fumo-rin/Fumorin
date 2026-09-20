using System;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace rinCore.Bullet
{
    #region Extended Actions & Utilities
    public partial class Projectile
    {
        public Projectile Action_Bounce(Vector2 normal, float bounce)
        {
            float speed = _regularVelocity.magnitude;
            _regularVelocity = _regularVelocity.Bounce(normal, bounce).ScaleToMagnitude(speed);
            return this;
        }

        public Projectile Action_AddRotation(float angle)
        {
            _regularVelocity = _regularVelocity.Rotate2D(angle);
            return this;
        }

        public Projectile Action_ModifySpeed(float multiplier)
        {
            _regularVelocity = _regularVelocity.ScaleToMagnitude(_regularVelocity.magnitude * multiplier);
            return this;
        }

        public Projectile Action_ShiftForward(float distance)
        {
            Vector2 dist = VelocityNotZero.ScaleToMagnitude(distance);
            SetNewPosition(_currentPosition + dist, false);
            return this;
        }

        public void AddForward(float forward)
        {
            if (forward != 0f)
            {
                Vector2 diff = VelocityNotZero.ScaleToMagnitude(forward);
                SetNewPosition(diff + _currentPosition, true);
            }
        }

        public Vector2 VelocityNotZero
        {
            get
            {
                if (_regularVelocity != Vector2.zero)
                {
                    return _regularVelocity;
                }
                return Vector2.down;
            }
        }

        public void SetNewPosition(Vector2 position, bool overrideLastPosition = false)
        {
            PreviousPosition = _currentPosition;
            if (overrideLastPosition)
            {
                PreviousPosition = position;
            }
            _currentPosition = position;
        }

        public static void ModifyVelocity(Projectile p, Vector2 newVelocity)
        {
            p._regularVelocity = newVelocity;
        }
    }
    #endregion
    #region Factory Helper
    public class ProjectileFactory
    {
        public static Projectile.ArcSettings Arc(float centerAimAngle, float arcSize, int shotCount, float projectileSpeed)
        {
            var result = new Projectile.ArcSettings(
                centerAimAngle - (arcSize * 0.5f),
                centerAimAngle + (arcSize * 0.5f),
                arcSize / Mathf.Clamp(shotCount - 1, 1, 9999),
                projectileSpeed);
            return result;
        }
        public static Projectile.SingleSettings Single(float addedAngle, float projectileSpeed)
            => new Projectile.SingleSettings(addedAngle, projectileSpeed);
        public static Projectile.CircleSettings Circle(float addedAngle, int segments, float projectileSpeed)
            => new Projectile.CircleSettings(addedAngle, segments, projectileSpeed);
    }
    #endregion
    public static class FactionExtension
    {
        public static bool IsFriendlyWith(this FumoUnit.UFaction fac, FumoUnit.UFaction other) => fac is not FumoUnit.UFaction.None && fac == other;
        public static bool IsHostileWith(this FumoUnit.UFaction fac, FumoUnit.UFaction other) => fac is FumoUnit.UFaction.None || fac != other;
    }
    public record RProj_Global_Clear(Rect? clear) : IRinEvent;
    public interface IParticleRenderItem
    {
        public bool SkipRender { get; }
        public Vector2 Render_Position { get; }
        public float Render_Angle { get; }
        public float Render_Size { get; }
    }
    public partial class Projectile : IParticleRenderItem
    {
        public struct HitPacket
        {
            public FumoUnit Sender;
            public Vector2 Point;
            public Vector2 Normal;
            public float Damage;
            public HitPacket(float damage, Vector2 position)
            {
                this.Sender = null;
                this.Point = position;
                this.Damage = damage;
                this.Normal = Vector2.down;
            }
        }
        public float FinalDamage => BaseDamage * (Sender is not null and IDamageMod mod ? mod.DamageMod : 1f);
        public float BaseDamage = 1f;
        [NonSerialized] public FumoUnit Sender;
        [NonSerialized] public ProjectileDefine data;
        public FumoUnit.UFaction Faction => Sender.AssignedFaction;
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
            public FumoUnit.UFaction PHitFaction => (this is FumoUnit f) ? f.AssignedFaction : FumoUnit.UFaction.None;
            public Transform HitTransform => (this is Component comp) ? comp.transform : null;

            public bool TryProjectileHit(Projectile.HitPacket packet, out float processedDealtDamage);
        }
        static RaycastHit2D[] hits = new RaycastHit2D[4];
        static HashSet<IProjectileHit> hitList = new();
        static ContactFilter2D batchContactFilter = new ContactFilter2D()
        {
            useLayerMask = true,
            useTriggers = false
        };
        public struct Settings
        {
            public LayerMask hitLayers;
            public RProj_Global_Clear GlobalClear;
            public Settings(LayerMask mask)
            {
                hitLayers = mask;
                GlobalClear = null;
            }
        }
        public static void ProcessBatch(IEnumerable<Projectile> projCollection, float dt, Settings settings, Action<IProjectileHit> extraHitAction)
        {
            batchContactFilter.SetLayerMask(settings.hitLayers);
            Rect? clearRect;
            foreach (var proj in projCollection)
            {
                if (!proj.IsValid)
                    continue;
                if (settings.GlobalClear != null && settings.GlobalClear.clear.HasValue)
                {
                    clearRect = settings.GlobalClear.clear.Value;
                    if (!clearRect.Value.Contains(proj.FinalizedPosition))
                    {
                        proj.IsValid = false;
                        //ProjectileRenderer.HitParticle(proj.FinalizedPosition, -proj.FinalizedVelocity);
                        continue;
                    }
                }

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

                        if (ihit.PHitFaction.IsFriendlyWith(proj.Faction))
                            continue;

                        if (proj.Sender == (object)ihit || !hitList.Add(ihit))
                            continue;


                        ProjectileRenderer.HitParticle(hit.point, hit.normal, new()
                        {
                            colorOverride = null,
                            forceMultiplier = 1f
                        });

                        if (ihit.TryProjectileHit(new()
                        {
                            Damage = proj.FinalDamage,
                            Sender = proj.Sender,
                            Normal = hit.normal,
                            Point = hit.point
                        }, out float hitActualDamage))
                        {

                        }
                        proj.IsValid = false;
                        extraHitAction?.Invoke(ihit);
                    }
                }
            }
        }
        public struct SweepPacket
        {
            public byte Loot;
            public float Duration;
        }
        public record FEB_Projectile_Sweep(SweepPacket packet) : IRinEvent;
        public record FEB_Projectile_Seal(SealPacket packet) : IRinEvent;
        public static void SweepAll(SweepPacket packet, Action<List<Projectile>> sweepAction = null)
        {
            ProjectileRunner.DestroyProjectiles(null, sweepAction);
            new FEB_Projectile_Sweep(packet).Publish();
        }
        public struct SealPacket
        {
            public SealPacket(FumoUnit Owner)
            {
                this.Owner = Owner;
                this.Distance = -1f;
                this.Loot = 255;
            }
            public FumoUnit Owner;
            public float Distance;
            public byte Loot;
        }
        public static void SealWith(SealPacket sweep, Action<List<Projectile>> sweepAction)
        {
            ProjectileRunner.DestroyProjectiles(x =>
            x.IsValid &&
            x.Sender == sweep.Owner &&
            sweep.Distance >= 0.05f && x.Sender.CurrentPosition.SquareDistanceToLessThan(x.FinalizedPosition, sweep.Distance)
            , sweepAction);
            new FEB_Projectile_Seal(sweep).Publish();
        }
        public struct BulletPacket
        {
            public ProjectileDefine Define;
            public FumoUnit Sender;
            public Vector2 Position;
            public Vector2 VelocityDirection;
            public float Damage;
        }
        public static Projectile BuildProjectile(BulletPacket b)
        {
            if (CreateProjectile(b.Define, b.Sender, b.Position, b.VelocityDirection, out Projectile newP))
            {
                newP.BaseDamage = b.Damage;
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