using UnityEngine;

namespace rinCore
{
    public class ShockwaveEffect : MonoBehaviour
    {
        const string positionKey = "_RingSpawnPosition";
        const string speedKey = "_ShockwaveSpeed";
        const string triggerTimeKey = "_ShockwaveTime";
        [SerializeField] Material shockwaveMaterial;
        static ShockwaveEffect current;
        private void Awake()
        {
            current = this;
            Trigger(new Vector2(0.5f, 0.5f), 0f);
            shockwaveMaterial.SetFloat(triggerTimeKey, -3f);
        }
        public static void Trigger(Vector2 screenspace01, float speed)
        {
            if (current == null || current.shockwaveMaterial is not Material shockwaveMaterial)
                return;
            shockwaveMaterial.SetFloat(speedKey, speed);
            shockwaveMaterial.SetVector(positionKey, screenspace01);
            shockwaveMaterial.SetFloat(triggerTimeKey, Time.time);
        }
    }
}
