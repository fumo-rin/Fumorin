using System.Collections;
using UnityEngine;

namespace rinCore
{
    public class SpriteFlashMaterial : MonoBehaviour
    {
        [SerializeField] SpriteMaterialQueue materialQueue;
        [ContextMenu("Activate Flash")]
        public void TriggerFlashMaterial(float duration)
        {
            materialQueue.RunMaterialQueue(duration);
        }
        private void WhenIframes(IPlayerIFrames.PlayerIframes frames)
        {
            TriggerFlashMaterial(frames.duration);
        }
        private void OnEnable()
        {
            RinBus.Bind<IPlayerIFrames.PlayerIframes>(WhenIframes);
        }
        private void OnDisable()
        {
            RinBus.Release<IPlayerIFrames.PlayerIframes>(WhenIframes);
        }
    }
}
