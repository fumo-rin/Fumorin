using Unity.Cinemachine;
using UnityEngine;

namespace rinCore
{
    public class CameraOffsetTarget : MonoBehaviour
    {
        [SerializeField] CinemachinePositionComposer positionComposer;
        private void OnEnable()
        {
            RinBus.Bind<FEB_Camera_Offset>(Apply);
        }
        private void OnDisable()
        {
            RinBus.Release<FEB_Camera_Offset>(Apply);
        }
        private void Apply(FEB_Camera_Offset offset)
        {
            positionComposer.TargetOffset = new(offset.x, offset.y, positionComposer.TargetOffset.z);
        }
    }
}
