using System;
using UnityEngine;
using Unity.AI.Navigation;

namespace rinCore
{
    [DefaultExecutionOrder(-50)]
    public class DynamicNavMeshProvider : MonoBehaviour
    {
        private static DynamicNavMeshProvider Instance;
        public static event Action OnNavMeshUpdated;
        public static bool IsUpdatingMesh => Instance != null && Instance.isUpdatingMesh;

        [SerializeField] private NavMeshSurface dynamicNavMesh;
        [SerializeField] private NavMeshSurface staticNavMesh;
        [SerializeField] private float updateMoveThreshold = 10f;

        private Vector3? lastBuildPosition;
        private AsyncOperation navMeshUpdateOp;
        private bool isUpdatingMesh;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (dynamicNavMesh != null)
            {
                dynamicNavMesh.transform.SetParent(null);
                dynamicNavMesh.transform.position = transform.position;
                dynamicNavMesh.center = Vector3.zero;
                dynamicNavMesh.size = new Vector3(150f, 50f, 150f);

                dynamicNavMesh.BuildNavMesh();
            }
            if (staticNavMesh != null)
            {
                staticNavMesh.transform.SetParent(null);

                staticNavMesh.BuildNavMesh();
            }

            lastBuildPosition = transform.position;
        }

        private void LateUpdate()
        {
            if (isUpdatingMesh && navMeshUpdateOp != null && navMeshUpdateOp.isDone)
            {
                isUpdatingMesh = false;
                navMeshUpdateOp = null;

                OnNavMeshUpdated?.Invoke();
            }

            if (!isUpdatingMesh && dynamicNavMesh != null && lastBuildPosition.HasValue && (transform.position - lastBuildPosition.Value).sqrMagnitude > (updateMoveThreshold * updateMoveThreshold))
            {
                dynamicNavMesh.transform.position = transform.position;
                var emptyData = new UnityEngine.AI.NavMeshData();
                dynamicNavMesh.navMeshData = emptyData;

                navMeshUpdateOp = dynamicNavMesh.UpdateNavMesh(emptyData);
                isUpdatingMesh = true;

                lastBuildPosition = transform.position;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}