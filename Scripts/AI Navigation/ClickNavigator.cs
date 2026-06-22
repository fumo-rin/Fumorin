using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.InputSystem;

namespace rinCore
{
    public class ClickNavigator : MonoBehaviour
    {
        [SerializeField] private Rigidbody rb;
        [SerializeField] private FumoNav nav;
        [SerializeField] private Camera overrideCam;
        [SerializeField] private NavMeshSurface navMesh;

        [Header("External Driver Settings")]
        [SerializeField] private bool autoRepath = true;
        [SerializeField] private float repathDistance = 2f;
        [SerializeField] private float repathRateLimit = 0.25f;

        private Vector3? lastBuildPosition;
        private Vector3 lastPathOrigin;
        private float repathCooldownTimer;

        [SerializeField] LayerMask clickMask;

        private AsyncOperation navMeshUpdateOp;
        private bool isUpdatingMesh;

        private Camera Cam => overrideCam != null ? overrideCam : Camera.main;

        private void Awake()
        {
            if (navMesh != null)
            {
                navMesh.transform.SetParent(null);
                navMesh.transform.position = transform.position;
                navMesh.center = Vector3.zero;
                navMesh.size = new Vector3(150f, 50f, 150f);
                navMesh.BuildNavMesh();
            }

            lastBuildPosition = transform.position;
        }

        private void LateUpdate()
        {
            HandleClick();

            if (isUpdatingMesh && navMeshUpdateOp != null && navMeshUpdateOp.isDone)
            {
                isUpdatingMesh = false;
                navMeshUpdateOp = null;

                if (nav.HasDestination)
                {
                    nav.SetDestination(transform.position, nav.Destination);
                }
            }

            if (!isUpdatingMesh && navMesh != null && lastBuildPosition.HasValue &&
                (transform.position - lastBuildPosition.Value).sqrMagnitude > 100f)
            {
                navMesh.transform.position = transform.position;

                navMeshUpdateOp = navMesh.UpdateNavMesh(navMesh.navMeshData);
                isUpdatingMesh = true;

                lastBuildPosition = transform.position;
            }

            if (repathCooldownTimer > 0f)
                repathCooldownTimer -= Time.deltaTime;

            HandleRepathTracking();
            nav.UpdateCornerProgress(transform.position);

            if (nav.HasReachedDestination(transform.position))
            {
                nav.StopPath();
                rb.linearVelocity = rb.VelocityTowardsXZ(Vector3.zero, 40f);
                return;
            }

            if (!nav.EvaluateMoveDirection(transform.position, out Vector3 moveDir))
            {
                rb.linearVelocity = rb.VelocityTowardsXZ(Vector3.zero, 40f);
                return;
            }
            Vector3 targetVelocity = moveDir * 5f;
            rb.linearVelocity = rb.VelocityTowardsXZ(targetVelocity, 40f);
        }
        private void HandleClick()
        {
            if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
                return;

            Camera cam = Cam;
            if (cam == null)
                return;

            Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (!Physics.Raycast(ray, out RaycastHit hit, 500f, clickMask, QueryTriggerInteraction.Ignore))
                return;

            if (nav.SetDestination(transform.position, hit.point))
            {
                lastPathOrigin = transform.position;
            }
        }
        private void HandleRepathTracking()
        {
            if (!autoRepath || !nav.HasDestination || repathCooldownTimer > 0f || isUpdatingMesh)
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
    }
}