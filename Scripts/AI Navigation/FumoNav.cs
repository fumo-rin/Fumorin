using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace rinCore
{
    [System.Serializable]
    public class FumoNav
    {
        #region AB Pathing
        private static float CalculateLength(NavMeshPath path)
        {
            float length = 0f;
            for (int i = 1; i < path.corners.Length; i++)
                length += Vector3.Distance(path.corners[i - 1], path.corners[i]);
            return length;
        }
        private static Vector3 Sample(NavMeshPath path, float distance)
        {
            if (path.corners.Length == 0) return Vector3.zero;
            if (distance <= 0f) return path.corners[0];

            for (int i = 1; i < path.corners.Length; i++)
            {
                Vector3 a = path.corners[i - 1];
                Vector3 b = path.corners[i];
                float segment = Vector3.Distance(a, b);
                if (distance <= segment)
                    return Vector3.Lerp(a, b, distance / segment);
                distance -= segment;
            }
            return path.corners[^1];
        }
        public Coroutine StartABPath(MonoBehaviour runner, Transform transform, Vector3 target, float maxSpeed, float minimumDuration, Action endAction, AnimationCurve pathInterpolation = null)
        {
            IEnumerator MoveRoutine()
            {
                var path = new NavMeshPath();
                if (!NavMesh.SamplePosition(transform.position, out var startHit, 5f, NavMesh.AllAreas)) yield break;
                if (!NavMesh.SamplePosition(target, out var endHit, 5f, NavMesh.AllAreas)) yield break;
                if (!NavMesh.CalculatePath(startHit.position, endHit.position, NavMesh.AllAreas, path)) yield break;
                if (path.status != NavMeshPathStatus.PathComplete) yield break;

                float length = CalculateLength(path);
                if (length <= 0f) yield break;

                float duration = Mathf.Max(minimumDuration, length / maxSpeed);
                float elapsed = 0f;

                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);
                    if (pathInterpolation != null)
                        t = pathInterpolation.Evaluate(t);

                    transform.position = Sample(path, length * t);
                    yield return null;
                }
                transform.position = path.corners[^1];
                endAction?.Invoke();
            }
            return runner.StartCoroutine(MoveRoutine());
        }
        #endregion

        [Header("Path Settings")]
        [SerializeField] private float waypointTolerance = 0.8f;
        [SerializeField] private float destinationTolerance = 1.0f;

        [Header("Stuck Recovery Settings")]
        [SerializeField] private float stuckThresholdDuration = 0.75f; // Seconds spent on one corner before recovery kicks in

        private NavMeshPath activePath;
        private NavMeshPath queryPath;

        private int currentCornerIndex;
        private bool hasDestination;
        private Vector3 destination;

        // Tracks progress to detect corner friction locks
        private int lastTrackedCornerIndex = -1;
        private float cornerTimeCounter = 0f;
        private Vector3 lastPosition;

        public bool HasDestination => hasDestination;
        public Vector3 Destination => destination;
        public int CurrentCornerIndex => currentCornerIndex;
        public Vector3[] PathCorners => (activePath != null && activePath.status != NavMeshPathStatus.PathInvalid) ? activePath.corners : System.Array.Empty<Vector3>();

        public float PathLength
        {
            get
            {
                float length = 0f;
                if (activePath == null || activePath.corners == null) return 0f;
                for (int i = 1; i < activePath.corners.Length; i++)
                    length += Vector3.Distance(activePath.corners[i - 1], activePath.corners[i]);
                return length;
            }
        }

        public void Reinitialize()
        {
            activePath = new NavMeshPath();
            queryPath = new NavMeshPath();
            ResetStuckTracking();
        }

        private void ResetStuckTracking()
        {
            lastTrackedCornerIndex = -1;
            cornerTimeCounter = 0f;
        }
        public bool SetDestination(Vector3 origin, Vector3 target, float projectionDistance = 5f)
        {
            if (!TryProjectToNavmesh(origin, out Vector3 projectedStart, projectionDistance))
            {
                if (!TryProjectToNavmesh(origin, out projectedStart, 10f))
                {
                    hasDestination = false;
                    return false;
                }
            }

            if (!TryProjectToNavmesh(target, out Vector3 projectedEnd, projectionDistance))
            {
                if (!TryProjectToNavmesh(target, out projectedEnd, 10f))
                {
                    hasDestination = false;
                    return false;
                }
            }

            NavMeshPath newPath = new();
            if (!NavMesh.CalculatePath(projectedStart, projectedEnd, NavMesh.AllAreas, newPath))
            {
                hasDestination = false;
                return false;
            }

            if (newPath.status == NavMeshPathStatus.PathInvalid)
            {
                hasDestination = false;
                return false;
            }

            activePath = newPath;

            destination = (newPath.status == NavMeshPathStatus.PathPartial && newPath.corners.Length > 0)
                ? newPath.corners[^1]
                : projectedEnd;

            currentCornerIndex = activePath.corners.Length > 1 ? 1 : 0;
            hasDestination = true;

            ResetStuckTracking();
            return true;
        }

        public void StopPath()
        {
            hasDestination = false;
            currentCornerIndex = 0;
            activePath = new NavMeshPath();
            ResetStuckTracking();
        }

        public void UpdateCornerProgress(Vector3 currentPosition)
        {
            if (!hasDestination || activePath.corners == null || activePath.corners.Length == 0)
                return;

            Vector3 posFlat = new Vector3(currentPosition.x, 0f, currentPosition.z);

            // Dynamic stuck checks
            if (currentCornerIndex == lastTrackedCornerIndex)
            {
                // If we aren't moving fast and are trying to reach the same corner, tick the stuck timer
                if (Vector3.SqrMagnitude(currentPosition - lastPosition) < 0.05f * Time.deltaTime)
                {
                    cornerTimeCounter += Time.deltaTime;
                }
            }
            else
            {
                lastTrackedCornerIndex = currentCornerIndex;
                cornerTimeCounter = 0f;
            }
            lastPosition = currentPosition;

            while (currentCornerIndex < activePath.corners.Length)
            {
                Vector3 targetCorner = activePath.corners[currentCornerIndex];
                Vector3 cornerFlat = new Vector3(targetCorner.x, 0f, targetCorner.z);
                float distance = Vector3.Distance(posFlat, cornerFlat);

                // 1. Standard distance check
                if (distance <= waypointTolerance)
                {
                    currentCornerIndex++;
                    cornerTimeCounter = 0f;
                    continue;
                }

                // 2. Dot Product skip check: If we have physically bypassed/passed the corner line, skip it 
                if (currentCornerIndex > 0)
                {
                    Vector3 prevCorner = activePath.corners[currentCornerIndex - 1];
                    Vector3 toCurrent = (targetCorner - prevCorner).normalized;
                    Vector3 playerToCurrent = (targetCorner - currentPosition).normalized;

                    // If dot product is negative, the agent has overshot/passed the waypoint line
                    if (Vector3.Dot(toCurrent, playerToCurrent) < -0.1f)
                    {
                        currentCornerIndex++;
                        cornerTimeCounter = 0f;
                        continue;
                    }
                }

                break;
            }
        }

        public bool HasReachedDestination(Vector3 currentPosition)
        {
            if (!hasDestination)
                return true;

            Vector3 posFlat = new Vector3(currentPosition.x, 0f, currentPosition.z);
            Vector3 destFlat = new Vector3(destination.x, 0f, destination.z);

            return Vector3.Distance(posFlat, destFlat) <= destinationTolerance;
        }
        public bool EvaluateMoveDirection(Vector3 currentPosition, out Vector3 direction)
        {
            direction = Vector3.zero;

            if (!hasDestination || activePath.corners == null || activePath.corners.Length == 0)
                return false;

            if (currentCornerIndex >= activePath.corners.Length)
                return false;

            Vector3 targetCorner = activePath.corners[currentCornerIndex];
            Vector3 posFlat = new Vector3(currentPosition.x, 0f, currentPosition.z);
            Vector3 cornerFlat = new Vector3(targetCorner.x, 0f, targetCorner.z);

            Vector3 rawDelta = cornerFlat - posFlat;
            if (rawDelta.sqrMagnitude < 0.01f)
                return false;

            Vector3 rawDirection = rawDelta.normalized;
            if (NavMesh.Raycast(currentPosition, targetCorner, out NavMeshHit hit, NavMesh.AllAreas))
            {
                Vector3 edgeNormal = hit.normal;
                edgeNormal.y = 0f;
                edgeNormal.Normalize();

                Vector3 slideDirection = Vector3.ProjectOnPlane(rawDirection, edgeNormal).normalized;

                direction = (slideDirection + (edgeNormal * 0.15f)).normalized;
            }
            else
            {
                direction = rawDirection;
            }

            return true;
        }

        #region NavMesh Queries
        public bool CanReach(Vector3 origin, Vector3 target)
        {
            if (!TryProjectToNavmesh(origin, out Vector3 start, 5f) || !TryProjectToNavmesh(target, out Vector3 end, 5f))
                return false;

            queryPath.ClearCorners();
            return NavMesh.CalculatePath(start, end, NavMesh.AllAreas, queryPath) && queryPath.status == NavMeshPathStatus.PathComplete;
        }

        public bool TryProjectToNavmesh(Vector3 position, out Vector3 navmeshPosition, float maxSearchDistance = 5f)
        {
            navmeshPosition = position;
            if (!NavMesh.SamplePosition(position, out NavMeshHit hit, maxSearchDistance, NavMesh.AllAreas))
                return false;

            navmeshPosition = hit.position;
            return true;
        }
        #endregion

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (activePath == null || activePath.corners == null || activePath.corners.Length < 2)
                return;

            Gizmos.color = Color.cyan;
            for (int i = 1; i < activePath.corners.Length; i++)
                Gizmos.DrawLine(activePath.corners[i - 1], activePath.corners[i]);

            if (hasDestination)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(destination, destinationTolerance);
            }

            if (currentCornerIndex < activePath.corners.Length)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(activePath.corners[currentCornerIndex], 0.2f);
            }
        }
#endif
    }
}