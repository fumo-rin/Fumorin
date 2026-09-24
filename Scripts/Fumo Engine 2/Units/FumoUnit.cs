using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using rinCore.Bullet;
using System;

namespace rinCore
{
    public record FEB_Unit_Death(FumoUnit unit, Vector2 position) : IRinEvent;
    public record FEB_Unit_PlayerDeath(FumoUnit unit, Vector2 position) : IRinEvent;
    public record FEB_Scoring_Unit_Damaged(bool player, FumoUnit unit, float damage) : IRinEvent;
    #region Unit Movers
    public enum MoveResult
    {
        Idle,
        Success,
        Failed,
        NotReady
    }
    public interface IUnitMover
    {
        public float MaxSpeed { get; set; }
        public MoveResult Move(FumoUnit unit, Vector2 input, ref float nextMoveTime);
    }
    public class FumoUnitMovers
    {
        public class PlayerMover : IUnitMover
        {
            public struct Settings
            {
                public float Acceleration, Friction;
                public Settings(float acceleration, float friction)
                {
                    this.Acceleration = acceleration;
                    this.Friction = friction;
                }
            }
            public float MaxSpeed { get; set; }
            Settings settings;

            public PlayerMover(float maxSpeed, Settings s)
            {
                this.MaxSpeed = maxSpeed;
                settings = s;
            }
            public MoveResult Move(FumoUnit unit, Vector2 input, ref float nextMoveTime)
            {
                MoveResult result = MoveResult.Failed;
                if (input == Vector2.zero)
                {
                    result = MoveResult.Idle;
                    unit.RB.VelocityTowards(Vector2.zero, settings.Friction);
                    return result;
                }
                if (Time.time > nextMoveTime)
                {
                    nextMoveTime = Time.time;
                    unit.RB.VelocityTowards(input.normalized.Clamp(0.15f, 1f) * MaxSpeed, settings.Acceleration);
                    result = MoveResult.Success;
                }
                else
                {
                    unit.RB.VelocityTowards(Vector2.zero, settings.Friction);
                    result = MoveResult.NotReady;
                }
                return result;
            }
        }
    }
    #endregion
    #region Unit Action
    public partial class FumoUnit
    {
        public bool IsRunningActions => CalculateRunningActions();
        private bool CalculateRunningActions()
        {
            if (actionTable == null || actionTable.Count <= 0)
            {
                return false;
            }
            foreach (var item in actionTable)
            {
                if (item.Value == null)
                    continue;
                if (item.Value.IsRunning())
                {
                    return true;
                }
            }
            return false;
        }
        Dictionary<string, UnitAction> actionTable = new();
        public FumoUnit SetAction(string key, UnitAction action)
        {
            if (actionTable.TryGetValue(key, out UnitAction a))
            {
                ClearAction(key);
            }
            if (action != null)
            {
                actionTable[key] = action;
            }
            return this;
        }
        public void ClearAllActions()
        {
            foreach (var item in actionTable.ToList())
            {
                ClearAction(item.Key);
            }
        }
        public void ClearAction(string key)
        {
            actionTable.Remove(key);
        }
    }
    #endregion
    #region Look Flip
    public partial class FumoUnit
    {
        public void FlipTowardsWorldPosition(Transform moveFlipAnchor, Vector2 target)
        {
            Vector2 input = target - CurrentPosition;
            if (moveFlipAnchor != null && input.normalized.x.Absolute() > 0.25f)
            {
                moveFlipAnchor.localScale =
                    new(input.x.Sign() * moveFlipAnchor.localScale.x.Absolute(),
                    moveFlipAnchor.localScale.y,
                    moveFlipAnchor.localScale.z);
            }
        }
        protected void FlipWithMovement(Vector2 input)
        {
            if (moveFlipAnchor is Transform t)
            {
                if (moveFlipAnchor != null && input.normalized.x.Absolute() > 0.25f)
                {
                    moveFlipAnchor.localScale =
                        new(input.x.Sign() * moveFlipAnchor.localScale.x.Absolute(),
                        moveFlipAnchor.localScale.y,
                        moveFlipAnchor.localScale.z);
                }
            }
        }
    }
    #endregion
    #region Mover
    public partial class FumoUnit
    {
        float nextMoveTime;
        [SerializeField] protected Animator moveAnimator;
        [SerializeField] protected string moveAnimatorStringKey = "MOVE";
        [SerializeField] protected Transform moveFlipAnchor;
        private IUnitMover baseUnitMover;
        public IUnitMover GetMover() => baseUnitMover;
        public IUnitMover SetMover(IUnitMover mover) => this.baseUnitMover = mover;
        public MoveResult PathMoveUnit(Vector2 input)
        {
            if (moveAnimator != null)
            {
                moveAnimator.SetBool(moveAnimatorStringKey, false);
            }
            if (baseUnitMover == null)
            {
                Debug.LogWarning("Trying to move unit without mover");
                return MoveResult.Failed;
            }
            MoveResult result = baseUnitMover.Move(this, input, ref nextMoveTime);
            if (moveAnimator != null)
            {
                moveAnimator.SetBool(moveAnimatorStringKey, false);
            }
            switch (result)
            {
                case MoveResult.Idle:
                    break;
                case MoveResult.Success:
                    if (input.sqrMagnitude > 0.25f)
                    {
                        if (moveAnimator != null)
                        {
                            moveAnimator.SetBool(moveAnimatorStringKey, true);
                        }
                        FlipTowardsWorldPosition(moveFlipAnchor, CurrentPosition + input);
                    }
                    break;
                case MoveResult.Failed:
                    break;
                case MoveResult.NotReady:
                    break;
                default:
                    break;
            }
            return result;
        }
    }
    #endregion
    #region Line of sight Scan
    public partial class FumoUnit
    {
        protected List<Vector2> scanPoints = new List<Vector2>()
        {
            new(-0.5f,-0.5f), new(0.5f, -0.5f), new(0f,0f), new(0.5f,0.5f), new(-0.5f, 0.5f)
        };
        public bool WeaponLineofSight(FumoUnit target, out FumoUnit result, float swingRange, LayerMask lineOfSight)
        {
            result = null;
            foreach (var p in scanPoints)
            {
                Vector2 scan = p + CurrentPosition;
                RaycastHit2D hit = Physics2D.Raycast(scan, target.CurrentPosition - scan, swingRange * 1.02f, lineOfSight);
                if (hit.transform == null)
                {
                    continue;
                }
                if (hit.transform.GetComponent<FumoUnit>() is FumoUnit hitTarget)
                {
                    result = hitTarget;
                    break;
                }
            }
            return result != null;
        }
    }
    #endregion
    #region Actions
    public abstract partial class FumoUnit
    {
        public FumoUnit Action_Teleport(Vector2 position)
        {
            if (Player == this)
            {
                Debug.Log("Teleported Player to : " + position.ToString("F2"));
            }
            transform.position = position;
            pather.ClearPath();
            ClearAllActions();
            rb.linearVelocity = Vector2.zero;
            return this;
        }
        public FumoUnit Action_PathTo(Vector2 d)
        {
            pather.StartPathing(d);
            return this;
        }
        public FumoUnit Action_ClearPath()
        {
            pather.ClearPath();
            return this;
        }
    }
    #endregion
    #region Enemy Collection & Cast
    public partial class FumoUnit
    {
        public abstract Cardinal Facing { get; }
        public static void ForceRemoveAliveEnemy(FumoUnit enemy)
        {
            if (aliveEnemies == null)
                return;
            aliveEnemies.Remove(enemy);
        }
        static HashSet<FumoUnit> aliveEnemies = new();
        public struct AliveSetterPacket
        {
            public bool ForceOverride;
            public bool OverrideAliveState;
            public AliveSetterPacket(bool forceoveride = false)
            {
                ForceOverride = forceoveride;
                OverrideAliveState = false;
            }
        }
        protected void MaintainAliveEnemy(FumoUnit unit, AliveSetterPacket packet)
        {
            if (unit == Player)
                return;
            if (packet.ForceOverride)
            {
                if (packet.OverrideAliveState)
                {
                    aliveEnemies.Add(unit);
                    return;
                }
                aliveEnemies.Remove(unit);
                return;
            }
            if (unit == null)
            {
                return;
            }
            if (unit.IsAlive)
            {
                aliveEnemies.Add(unit);
                return;
            }
            aliveEnemies.Remove(unit);
        }
        public bool TryAs<T>(T type) where T : Component
        {
            T result = null;
            if (this is T item)
            {
                result = item;
                return result;
            }
            return result != null;
        }
        public static IEnumerable<FumoUnit> AliveEnemies
        {
            get
            {
                if (aliveEnemies == null || aliveEnemies.Count < 1)
                {
                    yield break;
                }
                foreach (var item in aliveEnemies)
                {
                    if (item != null && item.gameObject != null && item.IsAlive && !item.exiting)
                        yield return item;
                }
            }
        }
        public struct AutoAimSettings
        {
            public Vector2 relativeAim;
            public float maxDot;
        }
        public static bool AutoAim(Vector2 point, AutoAimSettings settings, out FumoUnit result)
        {
            result = null;
            if (settings.relativeAim == Vector2.zero)
            {
                return false;
            }
            float bestDot = settings.maxDot;

            foreach (var unit in AliveEnemies)
            {
                Vector2 position = unit.CurrentPosition;
                Vector2 diff = position - point;

                if (diff == Vector2.zero)
                {
                    continue;
                }

                float dot = Vector2.Dot(settings.relativeAim.normalized, diff.normalized);

                if (dot > bestDot)
                {
                    bestDot = dot;
                    result = unit;
                }
            }

            return result != null;
        }
    }
    #endregion
    #region Inventory Swing Lock
    public partial class FumoUnit : IWeaponSwingLock
    {
        public float SwapLockEnd { get; set; }
        public float SwingLockEnd { get; set; }
    }
    #endregion
    #region Unit Faction
    public partial class FumoUnit
    {
        public UFaction AssignedFaction = UFaction.None;
        public enum UFaction
        {
            None = -1,
            Default = 0,
            Player = 100,
            Enemy = 200,

        }
        public bool IsFriendlyWith(UFaction other) => AssignedFaction.IsFriendlyWith(other);
        public bool IsHostileWith(UFaction other) => AssignedFaction.IsHostileWith(other);
    }
    #endregion
    #region Projectile Hit
    public partial class FumoUnit : Projectile.IProjectileHit
    {
        public bool TryProjectileHit(Projectile.HitPacket packet, out float processedDealtDamage)
        {
            bool hit = WhenProjectileHit(packet, out processedDealtDamage);
            if (processedDealtDamage > 0f)
            {
                new FEB_Scoring_Unit_Damaged(player: Player == this, this, processedDealtDamage).Publish();
            }
            return hit;
        }
        public abstract bool WhenProjectileHit(Projectile.HitPacket packet, out float processedDealtDamage);
    }
    #endregion
    #region MoveLerp
    public partial class FumoUnit
    {
        public Coroutine currentExternalMovement;
        public ExternalMovement cemData = new()
        {
            endTime = null,
            startTime = -1f
        };
        bool exiting = false;
        public void Action_CEM_Stop()
        {
            if (currentExternalMovement != null)
            {
                StopCoroutine(currentExternalMovement);
            }
        }
        public void Action_CEM_Exit(Vector2 direction, Rect? space, float delay, Action whenFinish)
        {
            IEnumerator Move_Out(Vector2 direction)
            {
                exiting = true;
                while (this != null && this.IsAlive)
                {
                    if (space.HasValue && !space.Value.Contains(this.CurrentPosition))
                    {
                        yield break;
                    }
                    rb.VelocityTowards(direction, 12f);
                    yield return null;
                }
            }
            IEnumerator CO_WaitAndRun()
            {
                yield return delay.WaitForSeconds();
                Action_CEM_Stop();
                cemData = new()
                {
                    startTime = Time.time,
                    endTime = -1f
                };
                currentExternalMovement = StartCoroutine(Move_Out(direction).Wrap(() => currentExternalMovement = null).Wrap(() => whenFinish?.Invoke()));
            }
            StartCoroutine(CO_WaitAndRun());
        }
        public void Action_CEM_Arbitrary(IEnumerator coroutine, float duration)
        {
            Action_CEM_Stop();
            cemData = new()
            {
                startTime = Time.time,
                endTime = Time.time + duration
            };
            currentExternalMovement = StartCoroutine(coroutine.Wrap(() => currentExternalMovement = null));
        }
        public void Action_CEM_A_to_B(Vector2 a, Vector2 b, float duration, System.Func<float> curve = null)
        {
            Action_CEM_Stop();
            cemData = new()
            {
                startTime = Time.time,
                endTime = Time.time + duration
            };
            IEnumerator CO_Run(float duration)
            {
                float durationRemain = duration;
                while (durationRemain > 0f)
                {
                    yield return null;
                }
            }
            currentExternalMovement = StartCoroutine(CO_Run(duration).Wrap(() => currentExternalMovement = null));
        }
    }
    #endregion
    #region Health
    public interface IFumoUnit_Health
    {
        public float CurrentHealth { get; }
        public float CurrentMaxHealth { get; }
        public float currentHealthPercent01 => CurrentMaxHealth <= 0 ? 0f : CurrentHealth / CurrentMaxHealth;
    }
    #endregion
    #region Action Frame & STG
    public record FEB_STG_ActionFrame(STG_Frame_Action actions = STG_Frame_Action.None) : IRinEvent;
    [System.Flags]
    public enum STG_Frame_Action
    {
        None = FlagsRaw_Int.None,
        Right = FlagsRaw_Int._1,
        Up = FlagsRaw_Int._2,
        Left = FlagsRaw_Int._3,
        Down = FlagsRaw_Int._4,
        ShootHeld = FlagsRaw_Int._5,
        ShootStarted = FlagsRaw_Int._6,
        ShootEnd = FlagsRaw_Int._7,
        FocusHeld = FlagsRaw_Int._8,
        FocusStarted = FlagsRaw_Int._9,
        FocusEnd = FlagsRaw_Int._10,
        FocusProcessed = FlagsRaw_Int._11,
        BombPressed = FlagsRaw_Int._12,
        HyperPressed = FlagsRaw_Int._13,
        Extra1 = FlagsRaw_Int._14,
        Extra2 = FlagsRaw_Int._15,
        Extra3 = FlagsRaw_Int._16,

        ShootAny = ShootHeld | ShootStarted,
        FocusAny = FocusHeld | FocusStarted,
        All = FlagsRaw_Int.All,
    }
    public interface IFumoUnit_STG_PlayerActionFrame
    {
        public FEB_STG_ActionFrame Frame { get; }
        public STG_Frame_Action STG_Action { get; }
        public Vector2 Movement
        {
            get
            {
                Vector2 m = Vector2.zero;
                if (STG_Action.Match(STG_Frame_Action.Right)) m += Cardinal.Right.Vec2();
                if (STG_Action.Match(STG_Frame_Action.Up)) m += Cardinal.Up.Vec2();
                if (STG_Action.Match(STG_Frame_Action.Left)) m += Cardinal.Left.Vec2();
                if (STG_Action.Match(STG_Frame_Action.Down)) m += Cardinal.Down.Vec2();
                return m;
            }
        }
    }
    #endregion
    public interface IDamageMod
    {
        public float DamageMod { get; }
    }
    #region IFrames
    public interface IUnitIframes
    {
        public record PlayerIframes(float duration, float endTime) : IRinEvent;
        public float IFramesRemaining { get; }
    }
    public partial class FumoUnit
    {
        public static bool IsPlayerIframesLessOrEqualTo(float maxFrames)
        {
            if (!FumoUnit.PlayerAs<FumoUnit>(out FumoUnit f))
                return false;

            return f.IsAlive && (!(f is IUnitIframes frames) || frames.IFramesRemaining <= maxFrames);
        }
        public static WaitUntil WaitForPlayerAliveWithIframes0_8 => new WaitUntil(() => IsPlayerIframesLessOrEqualTo(0.8f));
    }
    #endregion
    public interface IUnitCenter2
    {
        public FumoUnit CenterOwner { get; }
        public Vector2 Center { get; }
    }
    public abstract partial class FumoUnit : MonoBehaviour
    {
        protected void TriggerDeath()
        {
            if (this == Player)
            {
                new FEB_Unit_PlayerDeath(this, CurrentPosition).Publish();
                return;
            }
            new FEB_Unit_Death(this, CurrentPosition).Publish();
        }
        public abstract IEnumerable<Collider2D> Hitboxes { get; }
        public static FumoUnit Player { get; protected set; }
        public static bool PlayerAs<T>(out T player) where T : FumoUnit
        {
            if (Player is T p)
            {
                player = p;
                return player != null && p.gameObject != null;
            }
            else
            {

            }
            player = default;
            return false;
        }
        public bool UnitAs<T>(out T cast)
        {
            if (this is T p)
            {
                cast = p;
                return true;
            }
            else
            {

            }
            cast = default;
            return false;
        }
        public virtual void ForceKill()
        {
            gameObject.SetActive(false);
            MaintainAliveEnemy(this, new()
            {
                ForceOverride = true,
                OverrideAliveState = false
            });
            Destroy(gameObject);
        }
        public bool IsAlive => CalculateAlive();
        protected abstract bool CalculateAlive();
        public Vector2 NearestOnNavmeshOrCurrentPosition(float randomRange = 0, Vector2? offset = null)
        {
            Vector2 actualOffset = new(0f, 0f);
            if (offset != null)
            {
                actualOffset = offset.Value;
            }
            if (pather == null)
            {
                return CurrentPosition;
            }
            pather.GetNearestOnNavmesh(CurrentPosition + actualOffset + (randomRange > 0.05f ? RNG.SeededRandomInsideUnitCircle * randomRange : new(0f, 0f)), out Vector2 navmesh, randomRange * 2f);
            return navmesh;
        }
        public void SetPosition(Vector2 worldPosition)
        {
            transform.position = worldPosition;
        }
        public Vector2 CenterOrCurrentPosition => this is IUnitCenter2 c ? c.Center : CurrentPosition;
        public virtual Vector2 CurrentPosition
        {
            get
            {
                if (this is IUnitCenter2 c)
                {
                    return c.Center;
                }
                if (transform == null)
                {
                    return Vector2.zero;
                }
                return transform.position;
            }
        }
        public float UnitRadius => 0.5f;
        [SerializeField] Rigidbody2D rb;
        public Rigidbody2D RB => rb;
        Pather pather;
        private void Awake()
        {
            pather = Pather.Create(this, UnitRadius);
            WhenAwake();
        }
        private void Start()
        {
            WhenStart();
        }
        private void OnDestroy()
        {
            ClearAllActions();
            WhenDestroy();
        }
        private void Update()
        {
            if (this == null || !this.IsAlive)
            {
                return;
            }
            if (actionTable == null) actionTable = new();
            foreach (var item in actionTable.ToList())
            {
                if (item.Value != null)
                {
                    switch (item.Value.PerformAction())
                    {
                        case UnitAction.ActionResult.Cancelled:
                            ClearAction(item.Key);
                            break;
                        case UnitAction.ActionResult.Stall:
                            break;
                        case UnitAction.ActionResult.Performed:
                            break;
                        case UnitAction.ActionResult.End:
                            ClearAction(item.Key);
                            break;
                        default:
                            ClearAction(item.Key);
                            break;
                    }
                }
            }
            WhenUpdate(cemData);
        }
        private void OnDisable()
        {
            cemData = new()
            {
                startTime = -1f,
                endTime = null
            };
            WhenDisable();
            if (this.AssignedFaction == (UFaction.Enemy))
                MaintainAliveEnemy(this, new());
        }
        private void OnEnable()
        {
            SwingLockEnd = Time.time + 0.75f;
            SwapLockEnd = Time.time + 0.75f;
            WhenEnable();
            if (this.AssignedFaction == (UFaction.Enemy))
                MaintainAliveEnemy(this, new());
        }
        protected abstract void WhenAwake();
        protected abstract void WhenStart();
        protected abstract void WhenDestroy();
        protected abstract void WhenDisable();
        protected abstract void WhenEnable();
        public struct ExternalMovement
        {
            public float startTime;
            public float? endTime;
        }
        protected abstract void WhenUpdate(ExternalMovement movement);

    }
}