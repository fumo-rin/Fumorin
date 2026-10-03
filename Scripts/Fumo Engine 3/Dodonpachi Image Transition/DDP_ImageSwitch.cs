using System.Collections;
using UnityEngine;

namespace rinCore
{
    public record FEB_DDP_ImageSwitch(float startValue, float endValue, float duration) : IRinEvent;

    [DisallowMultipleComponent]
    public class DDP_ImageSwitch : MonoBehaviour
    {
        private static readonly int StatePropertyID = Shader.PropertyToID("_State");
        [SerializeField] private Material targetMaterial;
        public Material TargetMaterial
        {
            get => targetMaterial;
            set => targetMaterial = value;
        }
        private Coroutine current;
        private void OnEnable()
        {
            RinBus.Bind<FEB_DDP_ImageSwitch>(Run);
        }
        private void OnDisable()
        {
            RinBus.Release<FEB_DDP_ImageSwitch>(Run);
        }
        private void Run(FEB_DDP_ImageSwitch action)
        {
            if (targetMaterial == null) return;

            if (current != null)
            {
                StopCoroutine(current);
            }

            current = StartCoroutine(CO_Run(action).Wrap(() => current = null));

            IEnumerator CO_Run(FEB_DDP_ImageSwitch action)
            {
                float elapsed = 0f;
                float duration = Mathf.Max(action.duration, 0f);

                if (duration <= 0f)
                {
                    targetMaterial.SetFloat(StatePropertyID, action.endValue);
                    yield break;
                }

                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);

                    float currentValue = Mathf.Lerp(action.startValue, action.endValue, t);
                    targetMaterial.SetFloat(StatePropertyID, currentValue);

                    yield return null;
                }

                targetMaterial.SetFloat(StatePropertyID, action.endValue);
            }
        }
    }
}