using rinCore.Bullet;
using TMPro;
using UnityEngine;

namespace rinCore
{
    public class ProjectileCountDisplay : MonoBehaviour
    {
        [SerializeField] TMP_Text projectileCount;
        [SerializeField] string baseText = $"P: ";
        private void Process(FEB_Projectile_Count_Frame frame)
        {
            float speed = TimeSlowHandler.SimulatedSlowdown.Clamp(0.001f, 1f);
            float slowdownPercentVisual = (1f / speed * 100f).Clamp(100f, 999f);
            string slowdownText = $"[{slowdownPercentVisual.Floor().ToString("F0")}%]";
            projectileCount.text = baseText + frame.ProjectileCount.ToString().PadRight(5) + slowdownText;
        }
        private void OnEnable()
        {
            EventBus.Bind<FEB_Projectile_Count_Frame>(Process);
        }
        private void OnDisable()
        {
            EventBus.Release<FEB_Projectile_Count_Frame>(Process);
        }
    }
}
