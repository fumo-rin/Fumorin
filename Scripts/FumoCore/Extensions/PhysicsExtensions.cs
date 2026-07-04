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
    public static partial class PhysicsExtensions
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
}