using UnityEngine;

namespace rinCore
{
    public class RunnableObjectNavigator : MonoBehaviour
    {
        [SerializeField] public Rigidbody rb;
        [SerializeField] private BoxCollider boxCollider;
        [SerializeField] private FumoNav nav;

        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float stopBrakingSpeed = 40f;

        [Header("Internal Repathing Settings")]
        [SerializeField] private bool autoRepath = true;
        [SerializeField] private float repathDistance = 2f;
        [SerializeField] private float repathRateLimit = 0.25f;

        private Vector3 lastPathOrigin;
        public Vector3 LastFrameMoveVelocity { get; private set; }
        public bool HasPath => nav.HasDestination;
        private float repathCooldownTimer;

        public FumoNav Nav => nav;

        private void OnEnable()
        {
            DynamicNavMeshProvider.OnNavMeshUpdated += HandleNavMeshUpdated;
            nav.Reinitialize();
        }

        private void OnDisable()
        {
            DynamicNavMeshProvider.OnNavMeshUpdated -= HandleNavMeshUpdated;
        }

        private void LateUpdate()
        {
            if (repathCooldownTimer > 0f)
                repathCooldownTimer -= Time.deltaTime;

            bool isMeshUpdating = DynamicNavMeshProvider.IsUpdatingMesh;

            HandleRepathTracking(isMeshUpdating);
            nav.UpdateCornerProgress(transform.position);

            if (nav.HasReachedDestination(transform.position))
            {
                nav.StopPath();
                StopMovement();
                return;
            }
            if (isMeshUpdating || !nav.EvaluateMoveDirection(transform.position, out Vector3 moveDir))
            {
                StopMovement();
                return;
            }

            if (boxCollider != null)
            {
                boxCollider.HandleStepClimbing(rb, moveDir, 0.45f);
            }
            Vector3 targetVelocity = moveDir * moveSpeed;
            rb.linearVelocity = rb.VelocityTowardsXZ(targetVelocity, stopBrakingSpeed);
            LastFrameMoveVelocity = rb.linearVelocity.Y(0f);
        }

        public bool SetNewTarget(Vector3 targetPosition)
        {
            if (nav == null) return false;

            if (nav.SetDestination(transform.position, targetPosition))
            {
                lastPathOrigin = transform.position;
                return true;
            }
            return false;
        }

        public void StopMovement()
        {
            if (rb != null)
            {
                rb.linearVelocity = rb.VelocityTowardsXZ(Vector3.zero, stopBrakingSpeed);
            }
        }

        private void HandleRepathTracking(bool isMeshUpdating)
        {
            if (!autoRepath || !nav.HasDestination || repathCooldownTimer > 0f || isMeshUpdating)
                return;

            float sqrDistanceMoved = (transform.position - lastPathOrigin).sqrMagnitude;
            if (sqrDistanceMoved < repathDistance * repathDistance)
                return;

            repathCooldownTimer = repathRateLimit;
            if (nav.SetDestination(transform.position, nav.Destination))
            {
                lastPathOrigin = transform.position;
            }
        }

        private void HandleNavMeshUpdated()
        {
            if (nav != null && nav.HasDestination)
            {
                nav.SetDestination(transform.position, nav.Destination);
            }
        }
    }
}