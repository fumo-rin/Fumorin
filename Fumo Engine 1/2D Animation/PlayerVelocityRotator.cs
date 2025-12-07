using FumoCore.Tools;
using FumoCore.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Fumorin
{
    public class PlayerVelocityRotator : MonoBehaviour
    {
        [SerializeField] Transform rotationAnchor;
        [SerializeField] float weight = 3f;
        [SerializeField] float idleMultiplier = 2.5f;
        [SerializeField] Quaternion leftAngle;
        [SerializeField] Quaternion rightAngle;
        private void Update()
        {
            Vector2 input = ShmupInput.Move.QuantizeToStepSize(45f);

            Quaternion target = input.x.Absolute() < 0.2f ? Quaternion.identity : (input.x.Sign() > 0f ? rightAngle : leftAngle);
            bool idle = input.x == 0f;
            rotationAnchor.rotation = Quaternion.Lerp(rotationAnchor.rotation, target, Time.deltaTime * weight * (idle ? idleMultiplier : 1f));
        }
    }
}
