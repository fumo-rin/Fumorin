using UnityEngine;

namespace rinCore
{
    public class FollowIntegerLocked : MonoBehaviour
    {
        [SerializeField] Transform followTarget;
        [Range(0, 8), SerializeField] int subdivisions = 3;

        private void Awake()
        {
            if (followTarget != null)
                transform.SetParent(null);
        }

        private void Update()
        {
            if (followTarget == null)
                return;

            if (subdivisions == 0)
            {
                transform.position = followTarget.position.Int3();
                return;
            }

            Vector2 target = followTarget.position;
            float stepSize = 1 << subdivisions;
            target = target.Quantize(stepSize);

            transform.position = new Vector3(target.x, target.y, followTarget.position.z);
        }
    }
}