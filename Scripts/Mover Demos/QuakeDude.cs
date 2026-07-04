using UnityEngine;
using UnityEngine.InputSystem;

namespace rinCore
{
    public class QuakeDude : MonoBehaviour, IVelocity
    {
        [System.Serializable]
        public class QGrounded : IGroundCheck
        {
            [SerializeField] BoxCollider box;
            [SerializeField] LayerMask groundMask;
            public bool IsGrounded => box != null && groundedFrame(box);
            bool groundedFrame(BoxCollider box)
            {
                bool grounded = false;
                Bounds b = box.bounds;
                if (Physics.BoxCast(b.center + Vector3.up * 0.02f, new Vector3(b.extents.x * 0.95f, b.extents.y * 0.95f, b.extents.z * 0.95f), Vector3.down, out _, box.transform.rotation, 0.1f, groundMask, QueryTriggerInteraction.Ignore))
                {
                    grounded = true;
                }
                return grounded;
            }
        }
        [SerializeField] QuakeMotor m;
        [SerializeField] Rigidbody rb;
        [SerializeField] Transform cameraPivot;
        [SerializeField] QGrounded ground = new QGrounded();
        public Ray CameraRay => new(cameraPivot.position, cameraPivot.forward);

        public float sensitivity = 100f;
        public float Sensitivity
        {
            get
            {
                return sensitivity * 0.0001f;
            }
        }

        public Vector3 CurrentVelocity => rb.linearVelocity;
        Vector2 storedPortrait;
        public Vector2 PortraitRotation
        {
            get
            {
                return storedPortrait;
            }
        }

        float yaw;
        float pitch;

        [SerializeField] InputActionReference jumpAction;
        float lastJumpTime;
        private void Start()
        {
            ALHandler.CreateOrUpdate(cameraPivot);
            IVelocity.Player = this;
        }
        void Update()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            if (GeneralManager.IsPaused || Time.timeScale == 0f)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                return;
            }
            Vector2 look = GenericInput.MouseDeltaXY;
            storedPortrait = look.magnitude > 10f ? look.Sign() : Vector2.zero;
            yaw += look.x * Sensitivity;
            pitch -= look.y * Sensitivity;

            pitch = Mathf.Clamp(pitch, -89f, 89f);

            cameraPivot.localRotation = Quaternion.Euler(pitch, yaw, 0f);

            m.MoveOther(rb, GenericInput.Move, ground.IsGrounded);
            if (jumpAction.IsPressed() && ground.IsGrounded && !jumpAction.PressedLongerThan(0.4f) && lastJumpTime < Time.time + 0.15f)
            {
                rb.linearVelocity = new(rb.linearVelocity.x, 7f, rb.linearVelocity.z);
                lastJumpTime = Time.time;
                return;
            }
            if (!ground.IsGrounded && lastJumpTime + 0.05f < Time.time)
            {
                float y = rb.linearVelocity.y + (-24f * Time.deltaTime);
                y = y.Clamp(-30, 100f);
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, y, rb.linearVelocity.z);
            }
        }
    }
}
