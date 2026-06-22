using UnityEngine;
using UnityEngine.AI;

namespace rinCore
{
    public class FumoNav : MonoBehaviour
    {
        [Header("Path Settings")]
        [SerializeField] private float waypointTolerance = 0.8f;
        [SerializeField] private float destinationTolerance = 1.0f;

        private NavMeshPath activePath;
        private NavMeshPath queryPath;

        private int currentCornerIndex;
        private bool hasDestination;
        private Vector3 destination;

        public bool HasDestination => hasDestination;
        public Vector3 Destination => destination;
        public int CurrentCornerIndex => currentCornerIndex;
        public Vector3[] PathCorners => (activePath != null && activePath.status != NavMeshPathStatus.PathInvalid) ? activePath.corners : System.Array.Empty<Vector3>();

        private void Awake()
        {
            activePath = new NavMeshPath();
            queryPath = new NavMeshPath();
        }
        public bool SetDestination(Vector3 origin, Vector3 target, float projectionDistance = 5f)
        {
            if (!TryProjectToNavmesh(origin, out Vector3 projectedStart, projectionDistance))
                return false;

            if (!TryProjectToNavmesh(target, out Vector3 projectedEnd, projectionDistance))
                return false;

            activePath.ClearCorners();
            if (!NavMesh.CalculatePath(projectedStart, projectedEnd, NavMesh.AllAreas, activePath))
            {
                hasDestination = false;
                return false;
            }

            if (activePath.status == NavMeshPathStatus.PathInvalid)
            {
                hasDestination = false;
                return false;
            }

            destination = projectedEnd;
            currentCornerIndex = activePath.corners.Length > 1 ? 1 : 0;
            hasDestination = true;
            return true;
        }

        public void StopPath()
        {
            hasDestination = false;
            currentCornerIndex = 0;
        }
        public void UpdateCornerProgress(Vector3 currentPosition)
        {
            if (!hasDestination || activePath.corners == null || activePath.corners.Length == 0)
                return;

            Vector3 posFlat = new Vector3(currentPosition.x, 0f, currentPosition.z);

            while (currentCornerIndex < activePath.corners.Length)
            {
                Vector3 cornerFlat = new Vector3(activePath.corners[currentCornerIndex].x, 0f, activePath.corners[currentCornerIndex].z);

                if (Vector3.Distance(posFlat, cornerFlat) > waypointTolerance)
                    break;

                currentCornerIndex++;
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

            Vector3 delta = cornerFlat - posFlat;

            if (delta.sqrMagnitude < 0.01f)
                return false;

            direction = delta.normalized;
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