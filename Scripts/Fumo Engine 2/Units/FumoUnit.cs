using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RinCore
{
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
    public abstract partial class FumoUnit : MonoBehaviour
    {
        public static FumoUnit Player { get; protected set; }
        public static bool PlayerAs<T>(out T player)
        {
            if (Player is T p)
            {
                player = p;
                return true;
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
            pather.GetNearestOnNavmesh(CurrentPosition + actualOffset + (randomRange > 0.05f ? Random.insideUnitCircle * randomRange : new(0f, 0f)), out Vector2 navmesh, randomRange * 2f);
            return navmesh;
        }
        public void SetPosition(Vector2 worldPosition)
        {
            transform.position = worldPosition;
        }
        public Vector2 CurrentPosition => transform.position;
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
            WhenUpdate();
        }
        protected abstract void WhenAwake();
        protected abstract void WhenStart();
        protected abstract void WhenDestroy();
        protected abstract void WhenUpdate();
    }
}