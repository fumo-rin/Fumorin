using UnityEngine;
using DamageNumbersPro;
using System.Collections.Generic;
using Unity.Mathematics;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine.UIElements;
using System.Globalization;
using RinCore;
namespace RinCore
{
    public class DamageNumberWrapper
    {
        public DamageNumber n { get; private set; }
        public float spawnTime;
        float number;
        bool needsRebuild;
        public DamageNumberWrapper(DamageNumber number, float? spawnTime = null)
        {
            if (spawnTime == null)
            {
                this.spawnTime = Time.time;
            }
            else
            {
                this.spawnTime = spawnTime.Value;
            }
            n = number;
            needsRebuild = false;
        }
        public void SetFollow(Transform t)
        {
            n.SetFollowedTarget(t);
        }
        public void ClearSpawnTime()
        {
            spawnTime = 999f;
        }
        public void IncreaseNumber(float v, bool rebuild) => SetNumber(n.number + v, rebuild);
        public void SetNumber(float v, bool rebuild)
        {
            number = v;
            needsRebuild = true;
            if (rebuild) RebuildNumber(true);
        }
        public void SetNewLifeTime(float newTime)
        {
            n.lifetime = newTime;
        }
        public void RemoveLeftText()
        {
            n.enableLeftText = false;
        }
        public void RemoveRightText()
        {
            n.enableRightText = false;
        }
        public void SetLeftText(string t)
        {
            if (string.IsNullOrWhiteSpace(t))
            {
                n.leftText = " ";
                n.enableLeftText = true;
                n.UpdateText();
                return;
            }
            n.enableLeftText = true;
            n.leftText = t;
            n.UpdateText();
        }
        public void SetRightText(string t)
        {
            if (string.IsNullOrWhiteSpace(t))
            {
                n.rightText = " ";
                n.enableRightText = true;
                n.UpdateText();
                return;
            }
            n.enableRightText = true;
            n.rightText = t;
            n.UpdateText();
        }
        public void RebuildNumber(bool forced)
        {
            if (n == null || n.Equals(null))
                return;

            if ((needsRebuild || forced) && n != null)
            {
                n.number = number;
                n.UpdateText();
                needsRebuild = false;
            }
        }
        public float GetNumber()
        {
            return n.number;
        }
        public void SetString(string t)
        {
            n.GetTextMesh().text = t;
            n.UpdateText();
        }
        public void FadeIn()
        {
            n.FadeIn();
        }
        private static bool TryParseFlexibleFloat(string input, out float result)
        {
            result = 0;

            if (string.IsNullOrWhiteSpace(input))
                return false;

            input = input.Trim().ToLowerInvariant();

            // Remove plus signs and commas
            input = input.Replace("+", "").Replace(",", "");

            float multiplier = 1f;

            // Handle suffix multipliers
            if (input.EndsWith("k"))
            {
                multiplier = 1_000f;
                input = input.Substring(0, input.Length - 1);
            }
            else if (input.EndsWith("m"))
            {
                multiplier = 1_000_000f;
                input = input.Substring(0, input.Length - 1);
            }
            else if (input.EndsWith("b"))
            {
                multiplier = 1_000_000_000f;
                input = input.Substring(0, input.Length - 1);
            }

            // Try parsing the remaining number
            if (float.TryParse(input, System.Globalization.NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed))
            {
                result = parsed * multiplier;
                return true;
            }

            return false;
        }
    }
    [CreateAssetMenu(fileName = "New Damage Number", menuName = "Bremsengine/Damage Number")]
    public class DamageNumberSO : ScriptableObject
    {
        [SerializeField] DamageNumber numberPrefab;
        public bool Spawn(Vector2 position, float value, out DamageNumberWrapper wrapper)
        {
            wrapper = null;
            if (numberPrefab == null) return false;

            DamageNumber spawned = numberPrefab.Spawn(position, value);
            if (spawned != null)
                wrapper = new DamageNumberWrapper(spawned);

            return wrapper != null;
        }

        public bool Spawn(Vector2 position, double value, out DamageNumberWrapper wrapper)
            => Spawn(position, (float)value, out wrapper);

        public bool SpawnText(Vector2 position, string text, out DamageNumberWrapper wrapper)
        {
            wrapper = null;
            if (numberPrefab == null) return false;

            DamageNumber spawned = numberPrefab.Spawn(position, text);
            if (spawned != null)
                wrapper = new DamageNumberWrapper(spawned);

            return wrapper != null;
        }
    }
}
