using System;
using System.Collections.Generic;
using UnityEngine;

namespace rinCore.Bullet
{
    #region InputSettings Extensions
    public static class InputSettingsExtensions
    {
        public static Projectile.InputSettings SetOptionalTarget(this Projectile.InputSettings input, FumoUnit t)
        {
            input.OptionalTarget = t;
            return input;
        }

        /// <summary>
        /// Refresh Origin With Valid Sender or Override. Does nothing if invalid sender and no override.
        /// By struct ref.
        /// </summary>
        public static ref Projectile.InputSettings rf_ori(ref this Projectile.InputSettings input, Vector2? @override = null, Vector2? offset = null)
        {
            Vector2 basePosition;

            if (@override.HasValue)
            {
                basePosition = @override.Value;
            }
            else if (input.Sender != null)
            {
                basePosition = input.Sender.CurrentPosition;
            }
            else
            {
                return ref input;
            }

            input.Origin = basePosition + offset.GetValueOrDefault();
            return ref input;
        }

        /// <summary>
        /// Refresh Direction With Valid Sender or Override. Does nothing if invalid sender and no override.
        /// By struct ref.
        /// </summary>
        public static ref Projectile.InputSettings rf_dir(ref this Projectile.InputSettings input, Vector2? @override = null)
        {
            if (@override.HasValue)
            {
                input.Direction = @override.Value;
            }
            else if (input.Sender != null)
            {
                input.Direction = input.Sender.Facing.Vec2();
            }

            return ref input;
        }
        /// <summary>
        /// Refresh Direction as aim from origin to optional target.
        /// Does nothing if no optional target.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static ref Projectile.InputSettings rf_aim(ref this Projectile.InputSettings input)
        {
            if (input.OptionalTarget != null)
            {
                input.Direction = input.OptionalTarget.CenterOrCurrentPosition - input.Origin;
            }

            return ref input;
        }

        public static Projectile.InputSettings SetOrigin(this Projectile.InputSettings input, Vector2 position)
        {
            input.Origin = position;
            return input;
        }

        public static Projectile.InputSettings SetDirectionToTarget(this Projectile.InputSettings input, FumoUnit target)
        {
            if (target == null)
            {
                return input;
            }

            input.Direction = target.CurrentPosition - input.Origin;
            return input;
        }

        public static Projectile.InputSettings Reposition(this Projectile.InputSettings input)
        {
            if (input.Sender != null)
            {
                input.Origin = input.Sender.CurrentPosition;
            }

            return input;
        }

        public static Projectile.InputSettings ReAimWithOptionalTarget(this Projectile.InputSettings input, Vector2? origin = null)
        {
            if (origin.HasValue)
            {
                input.Origin = origin.Value;
            }

            if (input.OptionalTarget != null)
            {
                input.Direction = input.OptionalTarget.CurrentPosition - input.Origin;
            }

            return input;
        }

        public static Projectile.InputSettings SetDirection(this Projectile.InputSettings input, Vector2 direction)
        {
            input.Direction = direction;
            return input;
        }

        public static Projectile.InputSettings AimTo(this Projectile.InputSettings input, FumoUnit unit)
        {
            if (unit == null)
            {
                return input;
            }

            input.Direction = unit.CurrentPosition - input.Origin;
            return input;
        }

        public static Projectile.InputSettings AssignTarget(this Projectile.InputSettings input, FumoUnit target)
        {
            input.OptionalTarget = target;
            return input;
        }

        public static Projectile.InputSettings Rotate(this Projectile.InputSettings input, float r)
        {
            input.Direction = input.Direction.Rotate2D(r);
            return input;
        }
    }
    #endregion

    #region Input Settings & Pattern Spawning
    public partial class Projectile
    {
        public struct InputSettings
        {
            public List<IProjectileMod> Mods;
            public float BaseDamage;
            public FumoUnit Sender;
            public Vector2 Origin;
            public Vector2 OriginWithForward => Origin + Direction.ScaleToMagnitude(AddedForward);
            public Vector2 Direction;
            public FumoUnit OptionalTarget;
            public float AddedForward;

            public static void Auto(FumoUnit sender, FumoUnit.AutoAimSettings settings, out InputSettings input)
            {
                FumoUnit autoAim = null;

                if (FumoUnit.Player == sender)
                {
                    FumoUnit.AutoAim(sender.CenterOrCurrentPosition, settings, out autoAim);
                }
                else if (sender.AssignedFaction.Match(FumoUnit.UFaction.Enemy))
                {
                    autoAim = FumoUnit.Player;
                }

                input = new(
                    sender.CurrentPosition,
                    sender,
                    settings.relativeAim,
                    1f,
                    autoAim);
            }

            public InputSettings(
                Vector2 origin,
                FumoUnit sender,
                Vector2 direction,
                float baseDamage = 1f,
                FumoUnit optionalTarget = null,
                float addedForward = 0f)
            {
                Mods = null;
                BaseDamage = baseDamage;
                Sender = sender;
                Origin = origin;
                Direction = direction;
                OptionalTarget = optionalTarget;
                AddedForward = addedForward;
            }

            public InputSettings Copy()
            {
                return this;
            }
        }

        public struct SingleSettings
        {
            public float AddedAngle;
            public float ProjectileSpeed;

            public SingleSettings(float addedAngle, float projectileSpeed)
            {
                AddedAngle = addedAngle;
                ProjectileSpeed = projectileSpeed;
            }

            public bool Spawn(InputSettings input, ProjectileDefine define, out Projectile output)
            {
                return SpawnSingle(define, input, this, out output);
            }
        }

        public struct ArcSettings
        {
            public float StartingAngle { get; private set; }
            public float EndingAngle { get; private set; }
            public float ArcInterval { get; private set; }
            public float ProjectileSpeed { get; private set; }
            public bool IsReverse { get; private set; }

            public ArcSettings(float startingAngle, float arcEndAngle, float arcInterval, float projectileSpeed)
            {
                StartingAngle = startingAngle;
                EndingAngle = arcEndAngle;
                ArcInterval = arcInterval;
                ProjectileSpeed = projectileSpeed;
                IsReverse = false;
            }

            public static ArcSettings operator *(ArcSettings settings, float multiplier)
            {
                return new ArcSettings()
                {
                    StartingAngle = settings.StartingAngle,
                    EndingAngle = settings.EndingAngle,
                    ArcInterval = settings.ArcInterval / multiplier,
                    ProjectileSpeed = settings.ProjectileSpeed,
                    IsReverse = settings.IsReverse
                };
            }

            public ArcSettings Widen(float multiplier)
            {
                return new ArcSettings()
                {
                    StartingAngle = StartingAngle * multiplier,
                    EndingAngle = EndingAngle * multiplier,
                    ArcInterval = ArcInterval * multiplier,
                    ProjectileSpeed = ProjectileSpeed,
                    IsReverse = IsReverse
                };
            }

            public ArcSettings Speed(float multiplier)
            {
                return new ArcSettings()
                {
                    StartingAngle = StartingAngle,
                    EndingAngle = EndingAngle,
                    ArcInterval = ArcInterval,
                    ProjectileSpeed = ProjectileSpeed * multiplier,
                    IsReverse = IsReverse
                };
            }

            public ArcSettings Reverse()
            {
                return new ArcSettings()
                {
                    StartingAngle = StartingAngle,
                    EndingAngle = EndingAngle,
                    ArcInterval = ArcInterval,
                    ProjectileSpeed = ProjectileSpeed,
                    IsReverse = !IsReverse
                };
            }

            public IEnumerable<Projectile> SpawnForeach(InputSettings input, ProjectileDefine define)
            {
                if (!SpawnArc(define, input, this, out List<Projectile> output))
                {
                    yield break;
                }

                foreach (Projectile projectile in output)
                {
                    yield return projectile;
                }
            }

            public bool Spawn(InputSettings input, ProjectileDefine define, out List<Projectile> output)
            {
                return SpawnArc(define, input, this, out output);
            }
        }

        public struct CircleSettings
        {
            public float StartingAngle { get; private set; }
            public float ArcInterval { get; private set; }
            public float ProjectileSpeed { get; private set; }

            public CircleSettings(float startingAngle, int segments, float projectileSpeed)
            {
                StartingAngle = startingAngle;
                ArcInterval = 360f / Mathf.Max(segments, 2);
                ProjectileSpeed = projectileSpeed;
            }

            public bool Spawn(InputSettings input, ProjectileDefine define, out List<Projectile> output)
            {
                return SpawnCircle(define, input, this, out output);
            }

            public IEnumerable<Projectile> SpawnForeach(InputSettings input, ProjectileDefine define)
            {
                if (!SpawnCircle(define, input, this, out List<Projectile> output))
                {
                    yield break;
                }

                foreach (Projectile item in output)
                {
                    yield return item;
                }
            }
        }

        public static bool SpawnSingle(ProjectileDefine define, InputSettings input, SingleSettings settings, out Projectile output)
        {
            Vector2 rotatedDirection = input.Direction.Rotate2D(settings.AddedAngle);
            Vector2 offset = rotatedDirection.ScaleToMagnitude(input.AddedForward);
            Vector2 velocity = rotatedDirection.ScaleToMagnitude(settings.ProjectileSpeed);

            output = BuildProjectile(new BulletPacket
            {
                Define = define,
                Sender = input.Sender,
                Position = input.Origin + offset,
                VelocityDirection = velocity,
                Damage = input.BaseDamage,
                Mods = input.Mods
            });

            bool spawnedBullet = output != null;

            if (spawnedBullet && define.Flare)
            {
                ProjectileRenderer.BulletFlareParticle(
                    output.FinalizedPosition,
                    define.FlareColor,
                    output.FinalizedVelocity,
                    define.FlareSizeMod);
            }

            return spawnedBullet;
        }

        public static bool SpawnArc(ProjectileDefine define, InputSettings input, ArcSettings settings, out List<Projectile> output)
        {
            output = new();

            Vector2 offset;
            Vector2 rotatedDirection;
            float angle;

            foreach (float item in settings.ArcInterval.StepFromTo(settings.StartingAngle, settings.EndingAngle))
            {
                angle = item * (settings.IsReverse ? -1f : 1f);
                rotatedDirection = input.Direction.Rotate2D(angle);
                offset = rotatedDirection.ScaleToMagnitude(input.AddedForward);

                Projectile p = BuildProjectile(new BulletPacket
                {
                    Define = define,
                    Sender = input.Sender,
                    Position = input.Origin + offset,
                    VelocityDirection = rotatedDirection.normalized * settings.ProjectileSpeed,
                    Damage = input.BaseDamage,
                    Mods = input.Mods
                });

                if (p == null)
                {
                    continue;
                }

                if (define.Flare)
                {
                    ProjectileRenderer.BulletFlareParticle(
                        p.FinalizedPosition,
                        define.FlareColor,
                        p.FinalizedVelocity,
                        define.FlareSizeMod);
                }

                output.Add(p);
            }

            return output.Count > 0;
        }

        public static bool SpawnCircle(ProjectileDefine define, InputSettings input, CircleSettings settings, out List<Projectile> output)
        {
            ArcSettings s = new ArcSettings(
                -360f + settings.StartingAngle,
                settings.StartingAngle,
                settings.ArcInterval,
                settings.ProjectileSpeed);

            return SpawnArc(define, input, s, out output);
        }
    }
    #endregion
}