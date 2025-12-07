using UnityEngine;
using FumoCore.Tools;
using Pathfinding;
using Pathfinding.RVO;
using System;
using System.Collections;

namespace Fumorin.Units
{
    public class PathBuilder
    {
        public PathBuilder(Pather p)
        {
            pather = p;
        }
        public Pather pather;
        public void OnPathComplete(Path p)
        {
            if (!p.error)
            {
                pather.SetPath(p);
            }
        }
        public void PathTo(Vector2 start, Vector2 target)
        {
            if (pather.GetNearestOnNavmesh(target, out Vector2 navmeshPosition, 3f))
                pather.seeker.StartPath(start, navmeshPosition, OnPathComplete);
        }
    }
    public static class RVOExtension
    {
        public static Vector2 SolveRVO(this RVOController rvo, Vector2 direction, float maxSpeed)
        {
            if (rvo != null)
            {
                Vector2 velocity = direction.normalized * maxSpeed;
                rvo.Move(velocity);
                direction = rvo.CalculateMovementDelta(rvo.transform.position, Time.deltaTime);
            }
            return direction.normalized;
        }
    }
    [System.Serializable]
    public class Pather
    {
        PathBuilder pathBuilder;
        [field: SerializeField] public Seeker seeker { get; private set; }
        Vector2 position => owner.CurrentPosition;
        private Vector2 CurrentDirection => GetPathDirection(this.path, this.owner.CurrentPosition);
        public bool isAwaitingPath { get; private set; }
        FumoUnit owner;
        public Path path { get; private set; }
        int currentWaypoint;
        RVOController rvo;
        [SerializeField] float collapseDistance = 0.8f;
        public bool HasPath => path != null;
        private bool EndOfPath => path != null && !HasPath;
        public delegate void PathReachEndAction();
        private PathReachEndAction OnReachPathEnd;
        public bool GetNearestOnNavmesh(Vector2 position, out Vector2 navmeshPosition, float scanSize = 2f)
        {
            return CheckNavmeshPosition(position, out navmeshPosition, scanSize);
        }
        public static bool CheckNavmeshPosition(Vector2 position, out Vector2 navmeshPosition, float scanSize = 2f)
        {
            navmeshPosition = (Vector2)(Vector3)AstarPath.active.GetNearest(position, NearestNodeConstraint.Walkable);
            if (navmeshPosition.SquareDistanceToGreaterThan(position, scanSize))
            {
                return false;
            }
            return true;
        }
        public void BindReachPathEndAction(PathReachEndAction a)
        {
            OnReachPathEnd += a;
        }
        public void StartPathing(Vector2 target)
        {
            if (isAwaitingPath)
            {
                return;
            }
            OnReachPathEnd = null;
            SetAwaitingPath(true);
            pathBuilder.PathTo(seeker.transform.position, target);
        }
        public static Pather Create(FumoUnit unit, float patherRadius)
        {
            Pather p = new();
            p.owner = unit;
            p.ValidatePather(p.owner);
            p.AssignStuff(patherRadius);
            return p;
        }
        public Pather AssignStuff(float radius)
        {
            this.collapseDistance = radius + 0.1f;

            if (rvo == null)
            {
                if (owner.TryGetComponent(out RVOController found))
                {
                    rvo = found;
                }
                if (rvo == null && owner.GetComponentInChildren<RVOController>() is RVOController foundChild)
                {
                    rvo = foundChild;
                }
            }
            if (rvo != null)
            {
                rvo.radius = radius;
            }
            return this;
        }
        public bool PerformPath(out Vector2 pathDirection)
        {
            pathDirection = Vector2.zero;
            if (!HasPath)
            {
                return false;
            }
            Collapse();
            if (CurrentDirection == Vector2.zero)
            {
                return false;
            }
            IUnitMover m = owner.GetMover();
            if (m == null)
                return false;
            Vector2 rvoVector = rvo.SolveRVO(CurrentDirection, m.MaxSpeed);
            pathDirection = rvoVector;
            if (EndOfPath)
            {
                OnReachPathEnd?.Invoke();
            }
            return true;
        }
        public void ValidatePather(FumoUnit owner)
        {
            this.owner = owner;
            if (pathBuilder == null)
            {
                pathBuilder = new(this);
                pathBuilder.pather = this;
            }
        }
        public void Collapse()
        {
            int maxIterations = path.vectorPath.Count;
            int iterations = 0;

            while (currentWaypoint < path.vectorPath.Count - 1 && position.SquareDistanceToLessThan(path.vectorPath[currentWaypoint], collapseDistance))
            {
                currentWaypoint++;
                if (++iterations > maxIterations)
                {
                    Debug.LogWarning("Collapse loop exceeded expected iterations.");
                    break;
                }
            }
            if (currentWaypoint >= path.vectorPath.Count - 2)
            {
                if (position.SquareDistanceToLessThan(path.vectorPath[^1], owner.UnitRadius * 1.1f))
                {
                    ClearPath();
                }
            }
        }
        public void SetPath(Path p)
        {
            SetAwaitingPath(false);
            if (p == null)
            {
                path = null;
                currentWaypoint = 0;
                return;
            }
            if (!p.error)
            {
                path = p;
                currentWaypoint = 0;
                return;
            }
        }
        public Vector2 GetPathDirection(Path p, Vector2 position)
        {
            if (p == null || p.vectorPath.Count == 0)
                return Vector2.zero;
            if (p != null && p.vectorPath.Count == 1)
            {
                return (Vector2)p.vectorPath[0] - position.normalized;
            }
            return ((Vector2)p.vectorPath[currentWaypoint.Clamp(0, p.vectorPath.Count - 1)] - position).normalized;
        }
        public void SetAwaitingPath(bool state)
        {
            isAwaitingPath = state;
        }
        public void ClearPath() => SetPath(null);
    }
}