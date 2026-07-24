using UnityEngine;

namespace rinCore
{
    [System.Serializable]
    public class QGrounded : IGroundCheck
    {
        [SerializeField] BoxCollider box;
        public BoxCollider GroundedBox => box;
        [SerializeField] LayerMask groundMask;
        [SerializeField] string iceTag = "Ice";
        bool isOnIce;
        public float ForcedIcePhysicsEndTime;
        public bool IsOnIce => isOnIce || Time.time < ForcedIcePhysicsEndTime;
        public Vector3 LastGroundNormal { get; private set; } = Vector3.up;
        public float ForcedGroundedEndTime;
        public bool IsGrounded => Time.time < ForcedGroundedEndTime || (box != null && groundedFrame(box));
        bool groundedFrame(BoxCollider box)
        {
            isOnIce = false;
            bool grounded = false;
            Bounds b = box.bounds;

            if (Physics.BoxCast(b.center + Vector3.up * 0.02f, new Vector3(b.extents.x * 0.95f, b.extents.y * 0.95f, b.extents.z * 0.95f), Vector3.down, out RaycastHit hit, box.transform.rotation, 0.1f, groundMask, QueryTriggerInteraction.Ignore))
            {
                if (hit.transform.CompareTag(string.Intern(iceTag)))
                {
                    isOnIce = true;
                }

                LastGroundNormal = hit.normal;
                grounded = true;
            }
            else
            {
                LastGroundNormal = Vector3.up;
            }

            return grounded;
        }
    }
    [System.Serializable]
    public class QuakeMotor
    {
        [SerializeField] private Transform viewSocket;

        public MoveData settings = new();

        [System.Serializable]
        public class MoveData
        {
            public float MaxSpeed = 8f;
            public float Acceleration = 10f;
            public float Friction = 6f;
            public float StopSpeed = 2.5f;
            public float AirAcceleration = 1f;
            public float MaxOverspeed = 16f;
        }

        public void MoveOther(Rigidbody rb, Vector2 input, IGroundCheck grounded, out Vector2 relativePlanarXY)
        {
            float dt = Time.deltaTime;

            Vector3 velocity = rb.linearVelocity;
            Vector3 forward = viewSocket.transform.forward.Y(0f);
            Vector3 right = viewSocket.transform.right.Y(0f);

            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            Vector3 wishDir = forward * input.y + right * input.x;
            float wishSpeed = Mathf.Clamp01(input.magnitude) * settings.MaxSpeed;

            if (wishDir.sqrMagnitude > 0.0001f)
                wishDir.Normalize();

            if (grounded.IsGrounded)
                ApplyFriction(ref velocity, dt, grounded);

            float acceleration = grounded.IsGrounded ? settings.Acceleration : settings.AirAcceleration;
            if (grounded is QGrounded q && q.IsOnIce)
            {
                acceleration *= 0.125f;
            }
            Accelerate(ref velocity, wishDir, wishSpeed, acceleration, dt);

            ClampHorizontalSpeed(ref velocity);

            rb.linearVelocity = velocity;

            float relativeX = Vector3.Dot(velocity, right);
            float relativeY = Vector3.Dot(velocity, forward);

            relativePlanarXY = new Vector2(relativeX, relativeY);
        }

        private void ApplyFriction(ref Vector3 velocity, float dt, IGroundCheck grounded)
        {
            Vector3 lateral = new Vector3(velocity.x, 0f, velocity.z);

            float speed = lateral.magnitude;

            if (speed < 0.001f)
                return;

            float control = Mathf.Max(speed, settings.StopSpeed);
            float friction = settings.Friction;
            if (grounded is QGrounded q && q.IsOnIce)
            {
                friction *= 0.125f;
            }
            float drop = control * friction * dt;

            float newSpeed = Mathf.Max(speed - drop, 0f);

            if (newSpeed != speed)
                lateral *= newSpeed / speed;

            velocity.x = lateral.x;
            velocity.z = lateral.z;
        }

        private void Accelerate(ref Vector3 velocity, Vector3 wishDir, float wishSpeed, float acceleration, float dt)
        {
            if (wishDir.sqrMagnitude < 0.0001f)
                return;

            Vector3 lateral = new Vector3(velocity.x, 0f, velocity.z);
            float currentSpeed = Vector3.Dot(lateral, wishDir);
            float addSpeed = wishSpeed - currentSpeed;

            if (addSpeed <= 0f)
                return;

            float accelSpeed = acceleration * dt * wishSpeed;

            if (accelSpeed > addSpeed)
                accelSpeed = addSpeed;

            lateral += wishDir * accelSpeed;

            velocity.x = lateral.x;
            velocity.z = lateral.z;
        }

        private void ClampHorizontalSpeed(ref Vector3 velocity)
        {
            Vector3 lateral = new Vector3(velocity.x, 0f, velocity.z);

            float speed = lateral.magnitude;

            if (speed <= settings.MaxOverspeed)
                return;

            lateral *= settings.MaxOverspeed / speed;

            velocity.x = lateral.x;
            velocity.z = lateral.z;
        }
    }
}