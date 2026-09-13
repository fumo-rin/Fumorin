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
        private void WhenIframes(IUnitIframes.PlayerIframes frames)
        {
            TriggerFlashMaterial(frames.duration);
        }
        private void OnEnable()
        {
            EventBus.Bind<IUnitIframes.PlayerIframes>(WhenIframes);
        }
        private void OnDisable()
        {
            EventBus.Release<IUnitIframes.PlayerIframes>(WhenIframes);
        }
    }
}
