using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;
/*                                    =-@@@@@@@@@@@@@@@@@@                                          
                         @@@@@@@@@@@@@@@@@%@@@@@@@@@@@@@@@@@@@@                                
                         @######%##%%%%%%%%%%%%%@%%#########%@@@@@@                            
                         @@#%%%%###%########*+=+%@@@@@@@%########%@@@@@                        
                         @@%%%%%%##*+*+==---:--: .:-==#@@@@%#*%%%%###%@@@                      
                         @@%%####**+===-===+====++==--.  .+@@@%*#%##*##%@@@                    
                         @@##%#%%##*++=====++++++========-. .#@@@@@%%%##*@@@                   
                         @@%%%##*++-.:-:-::.:::::::::-:----=-.:*###%@@%%@@                     
                         @@###**+-:-----------------:::------::+*@%#@@@%@                      
 Headless                @@###**=.::-------------=-=---------::..-==#%%%@                      
Generic ish              @%###+=--::::-----:::::.::------::----::.-+%###@                      
    Projectile          @@#*#*-::::----:-:::::::::::::::::::::--:.:=%%%%@                      
         System         @@#+*+.:::::::-:-:-:----:::::::::::::::::.:-#%%#@                      
                       @@#*-=-:-:--::::::::--------::::::::::----:::*%%%@                      
  From                 @#*+-==-=-:-::--::-------:--::::-:.::---=-:-=*%%@@@                     
    Hell               @*++===-=-----==-:::::::::::-------:::---:--+@@@@@                      
                      @@+*+===-----:::--:..:::::::-:------::::--:-=@@                          
                      @@+#+-===++=++=-+*+-:::----:::::----------::+@-                          
    Death             @@#*+==-===:::--=+*+=----=-----:-=-:.:..:-:-#@                           
       Ahead.           @@%=====-=+*=-...:+*+-:=-:.:=--+=-::...:--=@@                           
                        *@@===--=#@@@@@@@#*##+*++=+***#@@#+===++=+@=                           
                          @=::---+**%#%%%#+=---===+#***#%%*+---==+@                            
                          @@%+=+*#*+*%@@#+---:-:::-*####**+===---@@                            
                          @+@%%#%@@@*-    ..........-==+*#%##%@@@@                             
                          @:..:.  .*@@@@@#+::::::====--:.. ..:=+*@                             
                         @@=.          =%@@@@@@@@@@@@*-..        -@@                           
                       @@@  .:++#@@@@@@@%%%##%%%%%%%@%####**=.     #@                          
                         @@@@@@@@@@##%%@@@%%##%%%%%%###%@@@@@@@#@@@@@                          
                         @@%#%%%##*#*******########*#******+*%@@@                              
                          @@%%##***###****#####*******#########@                               
                           @@@@%%##*#######***#***##%@@@@@@@@@#@@=                             
                            @@%%%%@@@@@%%#######*##%%#++++*+#%%#@@%                            
                            @@#%%####%%@@@@%##***#%#+-:....      :@@                           
                             @==-::.     .*%@@#*#**+=-.:...::...-@@@                           
                             @@-......:..  .=@@@###+--:::::::-=@@@                             
                              :@@==::::.  ....=@@#+:......::==%@                               
                               @@====-=*%*         .*@@@+-=-:..@@                              
                                @%:.::.:+#@@@@@@@@@@@  @@=::...+@                              
                                 @*. . ..=@%            .@:...:#@                              
                                 *@@@#:=@@               @@@@@@@@                              
                                     @@@@                  */
namespace rinCore.Bullet
{
    public record RSTG_LootList(List<Vector2> points, Vector2? senderPoint, SweepMode mode = SweepMode.Sweep) : IRinEvent;
    public record RProj_Global_Clear(Rect? clear) : IRinEvent;
    #region Events
    public record RSTG_Graze_Frame(int count, Vector2 firstPosition) : IRinEvent;
    public partial class Projectile
    {
    }
    #endregion
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
            return
              new Projectile.ArcSettings(
                centerAimAngle - (arcSize * 0.5f),
                centerAimAngle + (arcSize * 0.5f),
                arcSize / Mathf.Clamp(shotCount - 1, 1, 9999),
                projectileSpeed);
        }
        public static Projectile.SingleSettings Single(float addedAngle, float projectileSpeed)
            => new Projectile.SingleSettings(addedAngle, projectileSpeed);
        public static Projectile.CircleSettings Circle(float addedAngle, int segments, float projectileSpeed)
            => new Projectile.CircleSettings(addedAngle, segments, projectileSpeed);
    }
    #endregion
    #region Projectile Mod
    public interface IProjectileMod
    {
        public float Duration { get; }
        public bool Sequential { get; }
        public void Run(Projectile p, float deltaTime, ref float elapsed);
    }
    public partial class Projectile
    {
        struct appliedSweep
        {
            public HashSet<(float, byte)> sweeps;
            public float EndTime => sweeps?.DefaultIfEmpty().Max(x => x.Item1) ?? 0f;
            public byte Loot => sweeps?.DefaultIfEmpty().Max(x => x.Item2) ?? 0;
            public void ApplySweep(FEB_WhenProjectileSweep s)
            {
                sweeps ??= new();
                sweeps.RemoveWhere(x => Time.time >= x.Item1);
                sweeps.Add((s.packet.Duration + Time.time, s.packet.Loot));
            }
            public bool Sweeping => Time.time < EndTime;
        }
        static appliedSweep sweep;
        static Projectile()
        {
            RinBus.Bind<FEB_WhenProjectileSweep>((a) =>
            {
                sweep.ApplySweep(a);
            });
        }
        public List<IProjectileMod> mods = null;
        public partial struct Mods
        {
            public struct Accelerate : IProjectileMod
            {
                public float TargetSpeed;
                public float Acceleration;
                public float Duration;
                public bool Sequential;
                float IProjectileMod.Duration => Duration;
                bool IProjectileMod.Sequential => Sequential;
                public void Run(Projectile p, float deltaTime, ref float elapsed)
                {
                    Vector2 target = p._regularVelocity.ScaleToMagnitude(TargetSpeed);
                    p._regularVelocity = p._regularVelocity.MoveTowards(target, Acceleration);
                    elapsed += deltaTime;
                }
            }
            public struct DFK_Cancel : IProjectileMod
            {
                public float Duration;
                public float Radius;
                public FumoUnit.UFaction clearForFaction;
                float IProjectileMod.Duration => Duration;
                bool IProjectileMod.Sequential => Sequential;
                public bool Sequential;
                public bool ClearSenderProjectile;
                [ThreadStatic] static List<Vector2> points;
                public void Run(Projectile p, float deltaTime, ref float elapsed)
                {
                    if (!p.IsValid) return;
                    var self = this;

                    points ??= new List<Vector2>();
                    points.Clear();

                    ProjectileRunner.DestroyProjectiles(
                        cand => cand.IsValid
                             && cand.Faction.IsFriendlyWith(self.clearForFaction)
                             && cand.FinalizedPosition.SquareDistanceToLessThan(p.FinalizedPosition, self.Radius),

                        destroyedList =>
                        {
                            points.AddRange(destroyedList.Select(x => x.FinalizedPosition));
                            if (self.ClearSenderProjectile && destroyedList.Count > 0) p.IsValid = false;
                        });

                    if (points.Count > 0)
                    {
                        new RSTG_LootList(points, null, SweepMode.Seal).Publish();
                    }
                }
            }
            public struct Rotate : IProjectileMod
            {
                public float TotalArcSizeDegrees;
                public float Duration;
                public bool Sequential;
                float IProjectileMod.Duration => Duration;
                bool IProjectileMod.Sequential => Sequential;
                public void Run(Projectile p, float deltaTime, ref float elapsed)
                {
                    float step = TotalArcSizeDegrees / Duration;
                    p.Action_AddRotation(step * deltaTime);
                    elapsed += deltaTime;
                }
            }
            public struct Wait : IProjectileMod
            {
                public float Duration;

                float IProjectileMod.Duration => Duration;
                bool IProjectileMod.Sequential => true;
                public void Run(Projectile p, float deltaTime, ref float elapsed)
                {
                    elapsed += deltaTime;
                }
            }
        }
        public static void RunMods(Projectile p, float deltaTime)
        {
            if (p.mods == null || p.mods.Count == 0)
                return;

            float time = Time.time - p.spawnTime;
            float sequenceTime = 0f;

            for (int i = 0; i < p.mods.Count; i++)
            {
                IProjectileMod mod = p.mods[i];
                if (mod == null)
                    continue;

                float duration = Mathf.Max(0f, mod.Duration);

                if (mod.Sequential)
                {
                    float start = sequenceTime;
                    float end = start + duration;

                    if (time >= start && time < end)
                    {
                        float elapsed = Mathf.Clamp(time - start, 0f, duration);
                        mod.Run(p, elapsed - (elapsed - deltaTime), ref elapsed);
                    }

                    sequenceTime = end;
                }
                else
                {
                    if (time < 0f || time >= duration)
                        continue;

                    float elapsed = Mathf.Clamp(time, 0f, duration);
                    mod.Run(p, deltaTime, ref elapsed);
                }
            }
        }
    }
    #endregion

    public static class FactionExtension
    {
        public static bool IsFriendlyWith(this FumoUnit.UFaction fac, FumoUnit.UFaction other) => fac is not FumoUnit.UFaction.None && fac == other;
        public static bool IsHostileWith(this FumoUnit.UFaction fac, FumoUnit.UFaction other) => fac is FumoUnit.UFaction.None || fac != other;
    }
    public enum SweepMode
    {
        Dummy = 0,
        Sweep = 100,
        LingeringSweep = 200,
        Seal = 300,
    }
    public interface IParticleRenderItem
    {
        public bool SkipRender { get; }
        public Vector2 Render_Position { get; }
        public float Render_Angle { get; }
        public float Render_Size { get; }
    }
    public partial class Projectile : IParticleRenderItem
    {
        public static int CurrentIndex;
        public int SpawnIndex { get; private set; }
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
            useTriggers = true
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
        static HashSet<int> grazedProjectiles;
        public static void ProcessBatch(IEnumerable<Projectile> projCollection, float dt, Settings settings, Action<IProjectileHit> extraHitAction)
        {
            if (grazedProjectiles == null)
                grazedProjectiles = new();

            int grazeCount = 0;
            Vector2? firstGraze = null;
            bool graze = FumoUnit.PlayerAs(out FumoUnit player) && player.IsAlive;
            Vector2 fallbackGraze = player == null ? Vector2.zero : player.CenterOrCurrentPosition;

            // Make sure contact filter retains triggers!
            batchContactFilter.useTriggers = true;
            batchContactFilter.useLayerMask = true;

            foreach (var proj in projCollection)
            {
                if (!proj.IsValid)
                    continue;

                // Global Clear
                if (settings.GlobalClear != null && settings.GlobalClear.clear.HasValue)
                {
                    if (!settings.GlobalClear.clear.Value.Contains(proj.FinalizedPosition))
                    {
                        proj.IsValid = false;
                        continue;
                    }
                }

                // Graze check
                if (graze && !grazedProjectiles.Contains(proj.SpawnIndex) &&
                    proj.Faction.IsHostileWith(player.AssignedFaction) &&
                    proj.FinalizedPosition.SquareDistanceToLessThan(player.CenterOrCurrentPosition, 1.15f))
                {
                    firstGraze ??= proj.FinalizedPosition;
                    grazedProjectiles.Add(proj.SpawnIndex);
                    grazeCount++;
                }

                RunMods(proj, dt);

                Vector2 startPos = proj.FinalizedPosition;
                proj.PreviousPosition = startPos;
                Vector2 moveDelta = dt * proj.FinalizedVelocity;
                Vector2 endPos = startPos + moveDelta;
                proj._currentPosition = endPos;

                float travelDistance = moveDelta.magnitude;
                Vector2 castDirection = travelDistance > 0.0001f ? moveDelta / travelDistance : Vector2.zero;

                // Pass the settings.hitLayers directly so we don't accidentally ignore hurtbox layers!
                batchContactFilter.SetLayerMask(settings.hitLayers);

                int hitsCount = Physics2D.CircleCast(startPos, proj.data.CollisionRadius, castDirection, batchContactFilter, hits, travelDistance);

                for (int i = 0; i < hitsCount; i++)
                {
                    RaycastHit2D hit = hits[i];
                    Transform hitTrans = hit.transform;
                    if (hitTrans == null) continue;

                    // Check for target component
                    if (!hitTrans.TryGetComponent(out IProjectileHit ihit))
                    {
                        // If it hit terrain/environment wall without IProjectileHit
                        proj.IsValid = false;
                        ProjectileRenderer.HitParticle(hit.point - hit.normal.ScaleToMagnitude(.25f), hit.normal, new()
                        {
                            colorOverride = null,
                            forceMultiplier = 1f
                        });
                        break; // Stop checking hit array for this bullet
                    }

                    // Ignore friendly units
                    if (ihit.PHitFaction.IsFriendlyWith(proj.Faction) || proj.Sender == (object)ihit)
                        continue;

                    // Valid hostile hit!
                    ProjectileRenderer.HitParticle(hit.point, hit.normal, new()
                    {
                        colorOverride = null,
                        forceMultiplier = 1f
                    });

                    ihit.TryProjectileHit(new()
                    {
                        Damage = proj.FinalDamage,
                        Sender = proj.Sender,
                        Normal = hit.normal,
                        Point = hit.point
                    }, out _);

                    proj.IsValid = false;
                    extraHitAction?.Invoke(ihit);
                    break; // Bullet consumed!
                }
            }

            if (grazeCount > 0)
            {
                new RSTG_Graze_Frame(grazeCount, firstGraze ?? fallbackGraze).Publish();
            }
        }
        public struct SweepPacket
        {
            public byte Loot;
            public float Duration;
        }
        public record FEB_WhenProjectileSweep(SweepPacket packet) : IRinEvent;
        public static void SweepAll(SweepPacket packet, Action<List<Projectile>> sweepAction = null)
        {
            ProjectileRunner.DestroyProjectiles(null, sweepAction ?? new Action<List<Projectile>>(p =>
            {
                var pts = p?.ConvertAll(x => x.FinalizedPosition);
                if (pts?.Count > 0) new RSTG_LootList(pts, null, SweepMode.Sweep).Publish();
            }));
            new FEB_WhenProjectileSweep(packet).Publish();
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
        }
        public struct BulletPacket
        {
            public List<IProjectileMod> Mods;
            public ProjectileDefine Define;
            public FumoUnit Sender;
            public Vector2 Position;
            public Vector2 VelocityDirection;
            public float Damage;
        }
        public static Projectile BuildProjectile(BulletPacket b)
        {
            if (CreateProjectile(b, out Projectile newP))
            {
                newP.BaseDamage = b.Damage;
                newP.SpawnIndex = CurrentIndex++;
                return newP;
            }
            return null;
        }
        #region Sweeping Frame Buffer
        static List<Vector2> frontBuffer = new();
        static List<Vector2> backBuffer = new();
        public static bool GetLootFrame(out RSTG_LootList loot)
        {
            if (backBuffer.Count == 0)
            {
                loot = null;
                return false;
            }
            var temp = backBuffer;
            backBuffer = frontBuffer;
            frontBuffer = temp;

            backBuffer.Clear();
            loot = new RSTG_LootList(frontBuffer, null, SweepMode.LingeringSweep);
            return true;
        }
        #endregion
        static bool CreateProjectile(BulletPacket b, out Projectile p)
        {
            void Cancel(Vector2 position, Vector2 direction)
            {
                ProjectileRenderer.BulletCancelParticle(position, direction);
            }
            p = default;
            if (b.Define == null)
            {
                return false;
            }
            if (sweep.Sweeping && b.Sender.AssignedFaction.IsHostileWith(FumoUnit.UFaction.Player))
            {
                if (sweep.Loot > 0 && RNG.Byte255 < sweep.Loot)
                {
                    backBuffer.Add(b.Position);
                    Cancel(b.Position, b.VelocityDirection);
                }
                return false;
            }
            p = new Projectile
            {
                Sender = b.Sender,
                data = b.Define,
                _currentPosition = b.Position,
                PreviousPosition = b.Position,
                _regularVelocity = b.VelocityDirection,
                spawnTime = Time.time,
                animationOffsetSeconds = (1f / b.Define.animationSpeed) * (b.Define.animationSpreadPercent.RandomPositiveNegativeRange().Multiply(0.01f)),
                mods = b.Mods,
                IsValid = true
            };
            ProjectileRunner.InjectProjectile(p);
            return true;
        }
    }
}