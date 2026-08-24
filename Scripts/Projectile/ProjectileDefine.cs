using UnityEngine;

namespace rinCore.Bullet
{
    [CreateAssetMenu(menuName = "rinCore/Projectile/Define")]
    public class ProjectileDefine : ScriptableObject
    {
        public float animationSpeed;
        [Range(0f, 100)] public float animationSpreadPercent = 10f;
        public float spin;
        [SortingLayer] public string SortingLayer = "Default";
        [SerializeField] private ParticleSystem particleTemplate;
        [SerializeField] public float Size = 0.75f;
        [field: SerializeField] public bool LockRotation { get; private set; }

        #region Sorting Layer
        [Tooltip("Reasonable sorting Layer ranges are -200 to 0 for player and 800-1000 for Enemy." +
            "This value is used to manually tweak for edge cases")]
        [SerializeField] public int AddedSortingLayer = 0;
        public int sortingLayer => (Flare ? 1000 : 0) - Size.Multiply(100f).ToInt() + AddedSortingLayer;
        #endregion
        [field: SerializeField] public float CollisionRadius { get; private set; } = 0.05f;
        [field: SerializeField] public bool Flare { get; private set; } = true;
        [field: SerializeField] public Color32 FlareColor { get; private set; } = ColorHelper.PastelPurple.Opacity(255);

        public bool StartPS(out ParticleSystem ps)
        {
            ps = null;
            if (particleTemplate == null)
            {
                Debug.LogWarning($"ProjectileDefine '{name}': particleTemplate field is unassigned!");
                return false;
            }

            ps = Instantiate(particleTemplate);
            return ps != null;
        }
    }
}