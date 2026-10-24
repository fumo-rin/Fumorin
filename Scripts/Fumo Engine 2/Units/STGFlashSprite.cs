using UnityEngine;

namespace rinCore
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class STGFlashSprite : MonoBehaviour
    {
        [SerializeField] private string timeSetName = "_FlashTime";
        [SerializeField] private string durationName = "_FlashDuration";
        private Material instancedMaterial;
        float lockoutTime = 0.08f;
        float lastSetTime;
        private void Awake()
        {
            instancedMaterial = GetComponent<SpriteRenderer>().material;
            lastSetTime = -1f;
            lockoutTime = instancedMaterial.GetFloat(durationName) * 0.666f;
            instancedMaterial.SetFloat(timeSetName, -1f);
        }
        public void SetFlashTime(float value)
        {
            if ((value - lastSetTime).Absolute() <= lockoutTime)
                return;
            lastSetTime = value;
            instancedMaterial.SetFloat(timeSetName, value);
        }
    }
}