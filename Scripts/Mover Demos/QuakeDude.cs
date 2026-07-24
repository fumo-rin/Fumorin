using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace rinCore
{
    public class QuakeDude : MonoBehaviour, IVelocity
    {
        [field: SerializeField] public QGrounded ground { get; private set; } = new QGrounded();
        [SerializeField] BoxCollider stairClimber;
        [SerializeField] QuakeMotor m;
        [SerializeField] public Rigidbody rb;
        [SerializeField] Transform cameraPivot, projectilePivot, cameraRoll;
        [SerializeField] ACWrapper jumpHUUH;
        float roll = 0f;
        public Ray CameraRay => new(cameraPivot.position, cameraPivot.forward);
        public Transform LookTransform => cameraPivot;
        public Ray ProjectileShootRay => new(projectilePivot.position, projectilePivot.forward);

        public float sensitivity = 100f;
        public Slider sensitivtySlider;
        public TMP_Text sensText;
        string oldSensString;
        public float Sensitivity
        {
            get
            {
                return sensitivity;
            }
        }

        public void ResetVelocity()
        {
            rb.linearVelocity = Vector3.zero;
        }
        public Vector3 CurrentVelocity => rb.linearVelocity;
        public Vector3 CurrentPosition => transform.position;
        Vector2 storedPortrait;
        public Vector2 PortraitRotation
        {
            get
            {
                return storedPortrait;
            }
        }
        Vector2 storedPlanar;
        public Vector2 RelativePlanarMovementXY
        {
            get
            {
                return storedPlanar;
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
            sensitivtySlider.onValueChanged.AddListener((float v) =>
            {
                sensitivity = v;
                sensText.text = oldSensString + ": " + v.Multiply(10000f).ReverseQuantize(10f).ToString("F0");
                PersistentJSON.TrySave(sensitivity, "Player Sens");
            });
            string oldText = sensText.text.RemoveAfter(":");
            oldSensString = oldText;
            if (PersistentJSON.TryLoad(out float f, "Player Sens"))
            {
                sensitivtySlider.SetValues(f, 1f, 0.002f, true);
            }
            else
            {
                sensitivtySlider.SetValues(0.1f, 1f, 0.002f, true);
            }
        }
        private void OnDestroy()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            sensitivtySlider.onValueChanged.RemoveAllListeners();
        }

        #region Recoil
        float currentRecoil;
        Quaternion currentRecoilTarget;
        public void AddRecoil(float r, float max)
        {
            currentRecoil += r;
            currentRecoil = currentRecoil.Clamp(0f, max);

            float recoilPhase = RNG.FloatRange(0.4f, 1.2f);
            float yaw = Mathf.Sin(recoilPhase) * 12f;
            float pitch = -40f + Mathf.Sin(recoilPhase * 1.7f) * 12f;

            currentRecoilTarget = Quaternion.Euler(pitch, yaw, 0f);
        }
        void RecoilFrame(out Quaternion recoilOffset)
        {
            currentRecoil = currentRecoil.LerpTowards(0f, 2.8f * Time.deltaTime);
            recoilOffset = Quaternion.Slerp(Quaternion.Euler(0f, 0f, 0f), currentRecoilTarget, currentRecoil.MapTo01(0f, 90f));
        }
        #endregion
        public void MatchLook(Transform t)
        {
            Vector3 euler = t.rotation.eulerAngles;
            yaw = euler.y + 180f;
            pitch = euler.x;
            if (pitch > 180f) pitch -= 360f;

            pitch = Mathf.Clamp(pitch, -89f, 89f);
        }
        private void FixedUpdate()
        {
            stairClimber.HandleStepClimbing(rb, rb.linearVelocity, 0.25f, ground.LastGroundNormal, 45f);
        }
        float nextStepTime;
        void Update()
        {
            void Vertical()
            {
                if (jumpAction.IsPressed() && ground.IsGrounded && !jumpAction.PressedLongerThan(0.4f) && Time.time > lastJumpTime + 0.15f)
                {
                    rb.linearVelocity = new(rb.linearVelocity.x, 7f, rb.linearVelocity.z);
                    lastJumpTime = Time.time;
                    jumpHUUH.Play(CurrentPosition);
                    return;
                }
                if (!ground.IsGrounded && lastJumpTime + 0.05f < Time.time)
                {
                    float y = rb.linearVelocity.y + (-24f * Time.deltaTime);
                    y = y.Clamp(-30, 100f);
                    rb.linearVelocity = new Vector3(rb.linearVelocity.x, y, rb.linearVelocity.z);
                }
            }
            RecoilFrame(out Quaternion recoil);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            if (GeneralManager.IsPaused)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                return;
            }

            Vector2 look = GenericInput.Look;
            storedPortrait = look.magnitude > 10f ? look.Sign() : Vector2.zero;
            yaw += look.x * Sensitivity;
            pitch -= look.y * Sensitivity;

            pitch = Mathf.Clamp(pitch, -89f, 89f);

            cameraPivot.localRotation = Quaternion.Euler((recoil.x * 360f) + pitch, (-180f + recoil.y * 360f) + yaw, (recoil.z * 360f) + 0f);

            Vector2 input = GenericInput.Move;

            if (Time.time > nextStepTime && stairClimber.HandleStepClimbing(rb, rb.linearVelocity, 0.25f, ground.LastGroundNormal, 45f))
            {
                nextStepTime = Time.time + 0.04f;
                ground.ForcedGroundedEndTime = Time.time + 0.04f;
            }
            m.MoveOther(rb, input, ground, out storedPlanar);

            Vertical();

            if (cameraRoll)
            {
                float target = ground.IsGrounded ? input.x.Sign().Multiply(-5f).Clamp(-2.5f, 2.5f) : 0f;
                roll = roll.LerpTowards(target, ground.IsGrounded ? Time.deltaTime * 10f : Time.deltaTime * 10f);
                Quaternion r = cameraRoll.rotation;
                cameraRoll.rotation = Quaternion.Euler(r.eulerAngles.x, r.eulerAngles.y, roll);
            }
        }
    }
}
