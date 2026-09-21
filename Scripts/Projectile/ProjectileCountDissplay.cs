using rinCore.Bullet;
using TMPro;
using UnityEngine;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;

namespace rinCore
{
    public class ProjectileCountDisplay : MonoBehaviour
    {
        [SerializeField] TMP_Text projectileCount;
        [SerializeField] string baseText = $"P: ";
        FEB_Projectile_Count_Frame frame;
        RProj_Slowdown_PickupsCount pickups;
        private void ProcessPickups(RProj_Slowdown_PickupsCount p)
        {
            pickups = p;
        }
        private void Process(FEB_Projectile_Count_Frame frame)
        {
            this.frame = frame;
        }
        void LateUpdate()
        {
            float speed = TimeSlowHandler.SimulatedSlowdown.Clamp(0.001f, 1f);
            float slowdownPercentVisual = (1f / speed * 100f).Clamp(100f, 999f);
            string slowdownText = $"[{slowdownPercentVisual.Floor().ToString("F0")}%]";
            projectileCount.text = baseText + frame.ProjectileCount.ToString().PadRight(5) + slowdownText + $"\nI: {pickups.itemCount}";
        }
        private void OnEnable()
        {
            this.pickups = new()
            {
                itemCount = 0
            };
            frame = new(0);
            EventBus.Bind<FEB_Projectile_Count_Frame>(Process);
            EventBus.Bind<RProj_Slowdown_PickupsCount>(ProcessPickups);
        }
        private void OnDisable()
        {
            EventBus.Release<FEB_Projectile_Count_Frame>(Process);
            EventBus.Release<RProj_Slowdown_PickupsCount>(ProcessPickups);
        }
    }
}
