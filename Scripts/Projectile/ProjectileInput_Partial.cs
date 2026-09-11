using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

namespace rinCore.Bullet
{
    #region InputSettings Extensions
    public static class InputSettingsExtensions
    {
        public static Projectile.InputSettings SetOptionalTarget(this Projectile.InputSettings input, FumoUnit t)
        {
            return input.With(optionalTarget: t);
        }

        /// <summary>
        /// Refresh Origin With Valid Sender. Does nothing if invalid sender.
        /// By struct ref.
        /// </summary>
        public static ref Projectile.InputSettings rf_ori(ref this Projectile.InputSettings input, Vector2? @override = null)
        {
            if (input.Sender != null)
            {
                input = input.With(origin: @override ?? input.Sender.CurrentPosition);
            }
            return ref input;
        }

        /// <summary>
        /// Refresh Direction With Valid Sender. Does nothing if invalid sender.
        /// By struct ref.
        /// </summary>
        public static ref Projectile.InputSettings rf_Dir(ref this Projectile.InputSettings input)
        {
            if (input.Sender != null)
            {
                input = input.With(direction: input.Sender.Facing.Vec2());
            }
            return ref input;
        }

        public static Projectile.InputSettings SetOrigin(this Projectile.InputSettings input, Vector2 position)
        {
            return input.With(origin: position);
        }

        public static Projectile.InputSettings SetDirectionToTarget(this Projectile.InputSettings input, FumoUnit target)
        {
            if (target == null)
            {
                return input;
            }
            return input.With(direction: target.CurrentPosition - input.Origin);
        }

        public static Projectile.InputSettings Reposition(this Projectile.InputSettings input)
        {
            if (input.Sender != null)
            {
                return input.SetOrigin(input.Sender.CurrentPosition);
            }
            return input;
        }

        public static Projectile.InputSettings ReAimWithOptionalTarget(this Projectile.InputSettings input, Vector2? origin = null)
        {
            if (origin != null)
            {
                input = input.SetOrigin(origin.Value);
            }
            if (input.OptionalTarget != null)
            {
                input = input.SetDirectionToTarget(input.OptionalTarget);
            }
            return input;
        }

        public static Projectile.InputSettings SetDirection(this Projectile.InputSettings input, Vector2 direction)
        {
            return input.With(direction: direction);
        }

        public static Projectile.InputSettings AimTo(this Projectile.InputSettings input, FumoUnit unit)
        {
            if (unit == null)
                return input;
            return input.SetDirection(unit.CurrentPosition - input.Origin);
        }

        public static Projectile.InputSettings AssignTarget(this Projectile.InputSettings input, FumoUnit target)
        {
            return input.With(optionalTarget: target);
        }

        public static Projectile.InputSettings Rotate(this Projectile.InputSettings input, float r)
        {
            return input.With(direction: input.Direction.Rotate2D(r));
        }
    }
    #endregion

    #region Input Settings & Pattern Spawning
    public partial class Projectile
    {
        static List<Projectile> iterationList;

        public readonly struct InputSettings
        {
            public float BaseDamage { get; }
            public FumoUnit Sender { get; }
            public Vector2 Origin { get; }
            public Vector2 Direction { get; }
            public FumoUnit OptionalTarget { get; }
            public float AddedForward { get; }

            [NYI("Missing Auto Aim")]
            public static void Auto(FumoUnit sender, Vector2 direction, out InputSettings input)
            {
                FumoUnit autoAim = null;
                input = new(sender.CurrentPosition, sender, direction, 1f, autoAim);
            }

            public InputSettings(Vector2 origin, FumoUnit sender, Vector2 direction, float baseDamage = 1f, FumoUnit optionalTarget = null, float addedForward = 0f)
            {
                this.BaseDamage = baseDamage;
                this.Sender = sender;
                this.Origin = origin;
                this.Direction = direction;
                this.OptionalTarget = optionalTarget;
                this.AddedForward = addedForward;
            }

            public InputSettings With(
                float? baseDamage = null,
                FumoUnit sender = null,
                Vector2? origin = null,
                Vector2? direction = null,
                FumoUnit optionalTarget = null,
                float? addedForward = null)
            {
                return new InputSettings(
                    origin ?? this.Origin,
                    sender ?? this.Sender,
                    direction ?? this.Direction,
                    baseDamage ?? this.BaseDamage,
                    optionalTarget ?? this.OptionalTarget,
                    addedForward ?? this.AddedForward
                );
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
                this.AddedAngle = addedAngle;
                this.ProjectileSpeed = projectileSpeed;
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
                this.StartingAngle = startingAngle;
                this.EndingAngle = arcEndAngle;
                this.ArcInterval = arcInterval;
                this.ProjectileSpeed = projectileSpeed;
                this.IsReverse = false;
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
                    StartingAngle = this.StartingAngle * multiplier,
                    EndingAngle = this.EndingAngle * multiplier,
                    ArcInterval = this.ArcInterval * multiplier,
                    ProjectileSpeed = this.ProjectileSpeed,
                    IsReverse = this.IsReverse
                };
            }

            public ArcSettings Speed(float multiplier)
            {
                return new ArcSettings()
                {
                    StartingAngle = this.StartingAngle,
                    EndingAngle = this.EndingAngle,
                    ArcInterval = this.ArcInterval,
                    ProjectileSpeed = this.ProjectileSpeed * multiplier,
                    IsReverse = this.IsReverse
                };
            }

            public ArcSettings Reverse()
            {
                return new ArcSettings()
                {
                    StartingAngle = this.StartingAngle,
                    EndingAngle = this.EndingAngle,
                    ArcInterval = this.ArcInterval,
                    ProjectileSpeed = this.ProjectileSpeed,
                    IsReverse = !this.IsReverse
                };
            }

            public IEnumerable<Projectile> SpawnForeach(InputSettings input, ProjectileDefine define)
            {
                if (SpawnArc(define, input, this, out iterationList))
                {
                    foreach (Projectile projectile in iterationList)
                    {
                        yield return projectile;
                    }
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
                SpawnCircle(define, input, this, out iterationList);
                foreach (var item in iterationList)
                {
                    yield return item;
                }
            }
        }

        public static bool SpawnSingle(ProjectileDefine define, InputSettings input, SingleSettings settings, out Projectile output)
        {
            Vector2 offset = input.Direction.Rotate2D(settings.AddedAngle).ScaleToMagnitude(input.AddedForward);
            Vector2 velocity = input.Direction.Rotate2D(settings.AddedAngle).ScaleToMagnitude(settings.ProjectileSpeed);

            output = BuildProjectile(new BulletPacket
            {
                Define = define,
                Sender = input.Sender,
                Position = input.Origin + offset,
                VelocityDirection = velocity,
                Damage = input.BaseDamage
            });

            bool spawnedBullet = output != null;
            if (spawnedBullet && define.Flare)
            {
                ProjectileRenderer.BulletFlareParticle(output.FinalizedPosition + offset, define.FlareColor, output.FinalizedVelocity, define.FlareSizeMod);
            }
            return spawnedBullet;
        }

        public static bool SpawnArc(ProjectileDefine define, InputSettings input, ArcSettings settings, out List<Projectile> output)
        {
            output = new();
            Vector2 offset;
            Vector2 rotatedDirection;
            float angle;

            foreach (var item in settings.ArcInterval.StepFromTo(settings.StartingAngle, settings.EndingAngle))
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
                    Damage = input.BaseDamage
                });

                if (p == null)
                {
                    continue;
                }

                if (define.Flare)
                {
                    ProjectileRenderer.BulletFlareParticle(p.FinalizedPosition + offset, define.FlareColor, p.FinalizedVelocity, define.FlareSizeMod);
                }
                output.Add(p);
            }
            return output != null && output.Count > 0;
        }

        public static bool SpawnCircle(ProjectileDefine define, InputSettings input, CircleSettings settings, out List<Projectile> output)
        {
            ArcSettings s = new ArcSettings(-360f + settings.StartingAngle, settings.StartingAngle, settings.ArcInterval, settings.ProjectileSpeed);
            return SpawnArc(define, input, s, out output);
        }
    }
    #endregion
}