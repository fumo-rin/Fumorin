using UnityEngine;

namespace rinCore
{
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

        public void MoveOther(Rigidbody rb, Vector2 input, bool grounded, out Vector2 relativePlanarXY)
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

            if (grounded)
                ApplyFriction(ref velocity, dt);

            Accelerate(ref velocity, wishDir, wishSpeed, grounded ? settings.Acceleration : settings.AirAcceleration, dt);

            ClampHorizontalSpeed(ref velocity);

            rb.linearVelocity = velocity;

            float relativeX = Vector3.Dot(velocity, right);
            float relativeY = Vector3.Dot(velocity, forward);

            relativePlanarXY = new Vector2(relativeX, relativeY);
        }

        private void ApplyFriction(ref Vector3 velocity, float dt)
        {
            Vector3 lateral = new Vector3(velocity.x, 0f, velocity.z);

            float speed = lateral.magnitude;

            if (speed < 0.001f)
                return;

            float control = Mathf.Max(speed, settings.StopSpeed);
            float drop = control * settings.Friction * dt;

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