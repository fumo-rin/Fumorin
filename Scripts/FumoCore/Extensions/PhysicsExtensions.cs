using UnityEngine;

namespace rinCore
{
    #region Hitscan Impact
    #region Impact Struct
    public readonly struct Impact
    {
        public readonly Vector3 Point;
        public readonly Vector3 Direction;
        public readonly float Speed;
        public Vector3 Velocity => Direction * Speed;
        public Impact(Vector3 point, Vector3 direction, float speed)
        {
            Point = point;
            Direction = direction.normalized;
            Speed = speed;
        }
        public Impact(RaycastHit hit, Ray ray, float speed)
            : this(hit.point, ray.direction, speed)
        {
        }
    }
    #endregion
    public static partial class Physics3DExtensions
    {
        public static bool AddImpactVelocity(this Collider collider, Impact impact, float strength = 1f) =>
            collider.AddImpactVelocity(impact.Velocity, impact.Point, strength);

        private const float SoftVelocityLimit = 6f;
        private const float HardVelocityLimit = 11f;
        public static bool AddImpactVelocity(this Collider collider, Vector3 impactVelocity, Vector3 hitPoint, float strength = 1f)
        {
            if (!collider.TryGetComponent(out Rigidbody rb) || rb.isKinematic)
                return false;

            float speed = rb.linearVelocity.magnitude;
            float scale = 1f;
            if (speed > SoftVelocityLimit)
            {
                scale = Mathf.Sqrt(SoftVelocityLimit / speed);
                if (speed > HardVelocityLimit)
                {
                    float t = speed / HardVelocityLimit;
                    scale /= t * t;
                }
            }
            Vector3 impulse = impactVelocity * rb.mass * strength * scale;
            rb.AddForceAtPosition(impulse, hitPoint, ForceMode.Impulse);
            return true;
        }
    }
    #endregion
    #region Staircase Stepsize
    public static partial class Physics3DExtensions
    {
        public static void HandleStepClimbing(this BoxCollider box, Rigidbody rb, Vector3 moveDir, float maxStepHeight, float maxSlopeAngle = 45f)
        {
            if (moveDir.sqrMagnitude < 0.001f) return;

            Vector3 worldCenter = box.transform.TransformPoint(box.center);
            float extentsY = (box.size.y * 0.5f) * box.transform.lossyScale.y;
            Vector3 bottomPoint = worldCenter - new Vector3(0, extentsY, 0);
            Vector3 direction = new Vector3(moveDir.x, 0, moveDir.z).normalized;
            float contactCheckDistance = (box.size.z * 0.5f * box.transform.lossyScale.z) + 0.15f;
            Vector3 lowerOrigin = bottomPoint + Vector3.up * 0.02f;
            if (Physics.Raycast(lowerOrigin, direction, out RaycastHit hitLower, contactCheckDistance))
            {
                float surfaceAngle = Vector3.Angle(Vector3.up, hitLower.normal);
                if (surfaceAngle <= maxSlopeAngle) return;
                Vector3 upperOrigin = bottomPoint + Vector3.up * maxStepHeight;
                if (!Physics.Raycast(upperOrigin, direction, contactCheckDistance))
                {
                    Vector3 downwardRayOrigin = upperOrigin + (direction * contactCheckDistance);
                    if (Physics.Raycast(downwardRayOrigin, Vector3.down, out RaycastHit hitStepTop, maxStepHeight))
                    {
                        float actualStepHeight = hitStepTop.point.y - bottomPoint.y;
                        float topSurfaceAngle = Vector3.Angle(Vector3.up, hitStepTop.normal);
                        if (topSurfaceAngle > maxSlopeAngle) return;
                        if (actualStepHeight > 0.02f && actualStepHeight <= maxStepHeight)
                        {
                            rb.position += Vector3.up * actualStepHeight;
                            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
                        }
                    }
                }
            }
        }
    }
    #endregion
}