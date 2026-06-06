using System.Collections.Generic;
using UnityEngine;

namespace rinCore
{
    [System.Serializable]
    public class TransformGroundcheck2D : IGroundCheck, IGizmosDrawable
    {
        public bool IsGrounded
        {
            get
            {
                if (Time.time <= LastJumpTime + 0.1f)
                    return false;
                iteration = Physics2D.OverlapCircleAll(groundCheckPosition, radius, groundCollision);
                if (iteration == null || iteration.Length == 0)
                    return false;
                foreach (var item in iteration)
                {
                    if (item != null && !item.isTrigger)
                        return true;
                }
                return false;
            }
        }
        static HashSet<MonoBehaviour> groundCheckIteration;
        [SerializeField] Transform groundCheckAnchor;
        public Vector2 groundCheckPosition => groundCheckAnchor ? (Vector2)groundCheckAnchor.position : Vector2.zero;
        [SerializeField] float radius = 0.4f;
        [field: SerializeField] public float CoyoteTimeLength { get; private set; } = 0.15f;
        private float LastGroundedTime;
        [SerializeField] LayerMask groundCollision;
        private float LastJumpTime;
        private void DrawGizmo()
        {
            if (groundCheckAnchor == null)
                return;
            bool grounded = IsGrounded;
            Gizmos.color = grounded ? ColorHelper.PastelGreen : ColorHelper.PastelYellow;
            Gizmos.DrawWireSphere(groundCheckPosition, radius);
        }
        public bool HasCoyoteTime => CoyoteTimeLength + LastGroundedTime >= Time.time;
        static Collider2D[] iteration;
        public void SetJumpTimeNow() => LastJumpTime = Time.time;

        public void IDrawGizmos()
        {
            DrawGizmo();
        }

        public void IDrawGizmosSelected()
        {
            return;
        }
    }
}