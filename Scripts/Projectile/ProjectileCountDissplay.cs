using rinCore.Bullet;
using TMPro;
using UnityEngine;

namespace rinCore
{
    public class ProjectileCountDisplay : MonoBehaviour
    {
        [SerializeField] TMP_Text projectileCount;
        [SerializeField] string baseText = "P: ";
        private void Process(FEB_Projectile_Count_Frame frame)
        {
            projectileCount.text = baseText + frame.ProjectileCount;
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
