using rinCore.Bullet;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

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
                    Vector2 dir = packet.RawTarget - a;
                    Projectile.BuildProjectile(new()
                    {
                        Define = projectile,
                        Position = packet.Sender.CurrentPosition,
                        Sender = packet.Sender,
                        VelocityDirection = dir.ScaleToMagnitude(12f)
                    });
                }
            }
            [System.Serializable]
            public class BurstShot : WeaponScriptItemAction
            {
                public ProjectileDefine projectile;
                public int burstCount = 3;
                public ACWrapper sound;
                public override void RunItem(IFumoItem_Use.unitUsePacket packet)
                {
                    packet.Sender.StartCoroutine(CO_Run());
                    IEnumerator CO_Run()
                    {
                        Vector2 a = packet.Sender.CurrentPosition;
                        Vector2 dir = packet.RawTarget - a;
                        for (int i = 0; i < burstCount; i++)
                        {
                            a = packet.Sender.CurrentPosition;
                            Projectile p = Projectile.BuildProjectile(new()
                            {
                                Define = projectile,
                                Position = packet.Sender.CurrentPosition,
                                Sender = packet.Sender,
                                VelocityDirection = dir.ScaleToMagnitude(28f)
                            });
                            if (p != null && p.IsValid)
                            {
                                sound.Play(p.FinalizedPosition);
                            }

                            yield return 0.045f.WaitForSeconds();
                        }
                    }
                }
            }
            [System.Serializable]
            public class Bowap : WeaponScriptItemAction
            {
                public ProjectileDefine projectile;
                public override void RunItem(IFumoItem_Use.unitUsePacket packet)
                {
                    packet.Sender.StartCoroutine(CO_Run());
                    IEnumerator CO_Run()
                    {
                        float duration = 20f;
                        int iteration = 0;
                        while (packet.Sender.gameObject.activeInHierarchy && duration > 0)
                        {
                            for (int i = 0; i < 8; i++)
                            {
                                if (iteration % 2 == 1)
                                    continue;
                                float angle = iteration.AsFloat(0.1f).Pow(2f);
                                Vector2 a = packet.Sender.CurrentPosition;
                                Vector2 dir = Vector2.down.Rotate2D(-20f + angle + i.AsFloat(45f));
                                Projectile.BuildProjectile(new()
                                {
                                    Define = projectile,
                                    Position = packet.Sender.CurrentPosition,
                                    Sender = packet.Sender,
                                    VelocityDirection = dir.ScaleToMagnitude(8f)
                                });

                            }
                            iteration++;
                            duration -= Time.deltaTime;
                            yield return null;
                        }
                    }
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
