using rinCore.Bullet;
using UnityEngine;

namespace rinCore
{
    [CreateAssetMenu(menuName = "rinCore/Items/Weapon Script Item")]
    public class WeaponScriptItem : FumoItem, IFumoItem_Use
    {
        [System.Serializable]
        public abstract class WeaponScriptItemAction
        {
            [Range(-1f, 10f)] public float SwingCooldownTime = 0.65f;
            [Range(-1f, 10f)] public float SwapLockTime = 0.65f;
            public void ApplyTo(IFumoItem_WeaponItemSwing swing)
            {
                if (swing == null)
                    return;
                swing.SwingLockEnd = Time.time + SwingCooldownTime;
                swing.SwapLockEnd = Time.time + SwapLockTime;
            }
            public abstract void RunItem(IFumoItem_Use.unitUsePacket packet);
        }
        public partial class Testing
        {
            [System.Serializable]
            public class SingleShot : WeaponScriptItemAction
            {
                public ProjectileDefine projectile;
                public override void RunItem(IFumoItem_Use.unitUsePacket packet)
                {
                    Vector2 a = packet.Sender.CurrentPosition;
                    Vector2 dir = packet.Target - a;
                    Projectile.BuildProjectile(new()
                    {
                        Define = projectile,
                        Position = packet.Sender.CurrentPosition,
                        Sender = packet.Sender,
                        VelocityDirection = dir.ScaleToMagnitude(12f)
                    });
                }
            }
        }
        [SerializeReference, ManagedReferencePicker] WeaponScriptItemAction weaponScript;
        [SerializeField] ACWrapper sound;
        public bool TryUseHand(IFumoItem_Use.unitUsePacket packet)
        {
            if (weaponScript == null)
                return false;
            void UseSuccess(IFumoItem_Use.unitUsePacket packet, IFumoItem_WeaponItemSwing swing)
            {
                weaponScript.ApplyTo(swing);
                weaponScript.RunItem(packet);
            }
            IFumoItem_WeaponItemSwing swing = packet.Sender.GetComponentInChildren<IFumoItem_WeaponItemSwing>();
            bool hasSwing = swing != null;
            if (!hasSwing || !swing.SwingLock)
            {
                UseSuccess(packet, swing);
                sound.Play(RNG.SeededRandomVector2 * 15f + packet.Sender.CurrentPosition);
                return true;
            }
            return false;
        }
    }
}
