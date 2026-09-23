using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static rinCore.Bullet.Projectile;

namespace rinCore.Bullet
{
    public class Laser : MonoBehaviour
    {
        public LineRenderer Line;
        public LayerMask hitMask;
        [SerializeField] ParticleSystem muzzleTemplate;
        [NonSerialized] public Projectile.InputSettings Input;

        private Coroutine activeCoroutine;

        private readonly Dictionary<Transform, Laser> parentInstances = new();

        public struct Settings
        {
            public Transform parent;
            public float duration, fadeIn, fadeOut, maxLength, frameDamage, width;
        }

        public Laser Spawn(Projectile.InputSettings input, Settings settings)
        {
            Laser targetLaser = this;

            if (settings.parent != null)
            {
                if (!parentInstances.TryGetValue(settings.parent, out targetLaser) || targetLaser == null)
                {
                    targetLaser = Instantiate(this, settings.parent);
                    targetLaser.transform.localPosition = Vector3.zero;
                    targetLaser.transform.localRotation = Quaternion.identity;
                    parentInstances[settings.parent] = targetLaser;
                }
            }

            targetLaser.Input = input;

            if (targetLaser.Line != null && settings.parent != null)
            {
                targetLaser.Line.useWorldSpace = false;
            }

            if (targetLaser.muzzleTemplate)
            {
                targetLaser.muzzleTemplate.FC_PlayOneShotCached(input.OriginWithForward, input.Direction.ToRotation2D(Vector2.right, false));
            }

            if (targetLaser.activeCoroutine != null)
            {
                targetLaser.StopCoroutine(targetLaser.activeCoroutine);
            }

            targetLaser.activeCoroutine = targetLaser.StartCoroutine(targetLaser.CO_Lifecycle(settings));
            return targetLaser;
        }

        IEnumerator CO_Lifecycle(Settings s)
        {
            float remaining = s.duration;
            float fade;
            bool firstFrame = true;

            while (remaining >= 0f)
            {
                fade = Mathf.Min(s.fadeIn > 0f ? (s.duration - remaining) / s.fadeIn : 1f, s.fadeOut > 0f ? remaining / s.fadeOut : 1f);
                fade = Mathf.Clamp01(fade);

                if (Line != null)
                {
                    Color sCol = Line.startColor;
                    Color eCol = Line.endColor;
                    sCol.a = fade * fade * fade * fade;
                    eCol.a = fade * fade * fade * fade;
                    Line.startColor = sCol;
                    Line.endColor = eCol;

                    if (s.width > 0f)
                    {
                        Line.startWidth = s.width;
                        Line.endWidth = s.width;
                    }
                }

                if (firstFrame || fade >= 1f)
                {
                    firstFrame = false;

                    Vector2 startPos = s.parent != null ? (Vector2)transform.position : (Vector2)Input.OriginWithForward;
                    Vector2 dir = s.parent != null ? (Vector2)(s.parent.TransformDirection(Input.Direction)).normalized : Input.Direction.normalized;
                    float maxDist = s.maxLength > 0f ? s.maxLength : 1000f;

                    float laserWidth = s.width > 0f ? s.width : 0.05f;
                    float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

                    RaycastHit2D[] hits = Physics2D.BoxCastAll(startPos, new Vector2(0.01f, laserWidth), angle, dir, maxDist, hitMask);

                    Vector2 endPosWorld = startPos + dir * maxDist;
                    RaycastHit2D validHit = default;
                    bool foundValidHit = false;

                    for (int i = 0; i < hits.Length; i++)
                    {
                        RaycastHit2D candidateHit = hits[i];
                        if (candidateHit.collider == null)
                            continue;

                        Transform hitTrans = candidateHit.transform;

                        if (Input.Sender != null)
                        {
                            if (hitTrans == Input.Sender.transform || hitTrans.IsChildOf(Input.Sender.transform))
                                continue;

                            if (candidateHit.collider.gameObject == Input.Sender.gameObject)
                                continue;
                        }

                        if (candidateHit.transform.TryGetComponent(out IProjectileHit ihit))
                        {
                            if (Input.Sender == (object)ihit)
                                continue;
                        }

                        validHit = candidateHit;
                        foundValidHit = true;
                        break;
                    }

                    if (foundValidHit)
                    {
                        endPosWorld = validHit.point;
                        Transform hitTrans = validHit.transform;

                        if (!hitTrans.TryGetComponent(out IProjectileHit ihit))
                        {
                            ProjectileRenderer.HitParticle(validHit.point - validHit.normal.ScaleToMagnitude(.25f), validHit.normal, new()
                            {
                                colorOverride = null,
                                forceMultiplier = 1f
                            });
                        }
                        else if (!ihit.PHitFaction.IsFriendlyWith(Input.Sender.AssignedFaction) && Input.Sender != (object)ihit)
                        {
                            ProjectileRenderer.HitParticle(validHit.point, validHit.normal, new()
                            {
                                colorOverride = null,
                                forceMultiplier = 1f
                            });

                            if (ihit.TryProjectileHit(new()
                            {
                                Damage = s.frameDamage,
                                Sender = Input.Sender,
                                Normal = validHit.normal,
                                Point = validHit.point
                            }, out float hitActualDamage))
                            {

                            }
                        }
                    }

                    if (Line != null)
                    {
                        Line.positionCount = 2;
                        if (s.parent != null)
                        {
                            Line.useWorldSpace = false;
                            Line.SetPosition(0, Vector3.zero);

                            float actualDist = Vector2.Dot(endPosWorld - startPos, dir);
                            Line.SetPosition(1, Input.Direction.normalized * actualDist);
                        }
                        else
                        {
                            Line.useWorldSpace = true;
                            Line.SetPosition(0, startPos);
                            Line.SetPosition(1, endPosWorld);
                        }
                    }
                }

                remaining -= Time.deltaTime;
                yield return (1f / 60f).WaitForSeconds();
            }

            activeCoroutine = null;

            if (s.parent != null)
            {
                if (Line != null)
                {
                    Line.positionCount = 0;
                }
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}