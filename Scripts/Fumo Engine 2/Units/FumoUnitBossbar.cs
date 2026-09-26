using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace rinCore
{
    public record FEB_Unit_AssignBossBar(FumoUnit unit, string bossName) : IRinEvent;

    #region Boss Registration
    public partial class FumoUnitBossbar
    {
        private void RegisterBoss(FEB_Unit_AssignBossBar assignment)
        {
            if (assignment?.unit == null)
                return;

            bossNameLookup[assignment.unit] = assignment.bossName;
            damageTable[assignment.unit] = new RollingFloatTracker(windowSeconds: 3f, intervalSeconds: 0.1f, emaSmoothing: 25f);
        }
        private void CleanupDeadUnits()
        {
            if (damageTable.Count == 0)
                return;

            List<FumoUnit> removeList = null;

            foreach (var item in damageTable)
            {
                if (item.Key == null)
                {
                    removeList ??= new();
                    removeList.Add(item.Key);
                    continue;
                }

                IFumoUnit_Health health = item.Key as IFumoUnit_Health;

                if (health != null && health.CurrentHealth <= 0f)
                {
                    removeList ??= new();
                    removeList.Add(item.Key);
                }
            }

            if (removeList == null)
                return;

            foreach (var unit in removeList)
            {
                damageTable.Remove(unit);
                bossNameLookup.Remove(unit);

                if (activeBoss == unit)
                    activeBoss = null;
            }
        }
    }
    #endregion
    #region Boss Display
    public partial class FumoUnitBossbar
    {
        private void UpdateBar()
        {
            FumoUnit prio = null;
            float highest = 0f;

            foreach (var item in damageTable)
            {
                if (item.Key == null || item.Value == null)
                    continue;

                float dps = item.Value.EMA_PerSecond;

                if (dps > highest)
                {
                    highest = dps;
                    prio = item.Key;
                }
            }

            activeBoss = prio;

            if (activeBoss == null)
            {
                if (healthSlider != null)
                {
                    healthSlider.SetValues(0f, 1f, 0f, false);
                    healthSlider.value = 0f;
                }

                return;
            }

            if (bossNameText != null && bossNameLookup.TryGetValue(activeBoss, out string bossName))
            {
                bossNameText.text = bossName;
            }

            if (healthSlider != null)
            {
                IFumoUnit_Health health = activeBoss as IFumoUnit_Health;

                float current = health != null ? health.CurrentHealth : 0f;
                float max = health != null && health.CurrentMaxHealth > 0f
                    ? health.CurrentMaxHealth
                    : 1f;

                healthSlider.SetValues(current, max, 0f, false);
                healthSlider.value = current;
            }
        }
        private void AnimateUI()
        {
            bool hasTarget = activeBoss != null && damageTable.ContainsKey(activeBoss);

            Vector2 targetSize = hasTarget ? expandedSize : collapsedSize;
            float targetAlpha = hasTarget ? 1f : 0f;
            float currentFadeSpeed = hasTarget ? fadeInSpeed : fadeOutSpeed;

            if (bossContainer != null)
            {
                bossContainer.sizeDelta = Vector2.Lerp(
                    bossContainer.sizeDelta,
                    targetSize,
                    Time.deltaTime * sizeLerpSpeed
                );
            }

            if (bossCanvasGroup != null)
            {
                bool isTiny = bossContainer != null &&
                              bossContainer.sizeDelta.y <= expandedSize.y * fadeOutSizeThreshold;

                if (!hasTarget && isTiny)
                {
                    bossCanvasGroup.alpha = 0f;
                }
                else
                {
                    bossCanvasGroup.alpha = Mathf.Lerp(
                        bossCanvasGroup.alpha,
                        targetAlpha,
                        Time.deltaTime * currentFadeSpeed
                    );
                }
            }
        }
    }
    #endregion
    #region Events
    public partial class FumoUnitBossbar
    {
        private void UnitDeath(FEB_Unit_Death action)
        {
            if (action.unit == null)
                return;

            bossNameLookup.Remove(action.unit);
            damageTable.Remove(action.unit);

            if (activeBoss == action.unit)
                activeBoss = null;
        }

        private void UnitDamaged(FEB_Unit_Damaged action)
        {
            if (action.unit == null || !bossNameLookup.ContainsKey(action.unit))
                return;

            damageTable[action.unit].Record(action.damage);
        }
    }
    #endregion
    [DefaultExecutionOrder(-100)]
    public partial class FumoUnitBossbar : MonoBehaviour
    {
        [SerializeField] RectTransform bossContainer;
        [SerializeField] CanvasGroup bossCanvasGroup;
        [SerializeField] Slider healthSlider;
        [SerializeField] TMP_Text bossNameText;

        const float sizeLerpSpeed = 10f;
        const float fadeInSpeed = 3.5f;
        const float fadeOutSpeed = 1.75f;
        const float fadeOutSizeThreshold = 0.1f;

        Vector2 expandedSize;
        Vector2 collapsedSize;

        Dictionary<FumoUnit, string> bossNameLookup = new();
        Dictionary<FumoUnit, RollingFloatTracker> damageTable = new();

        FumoUnit activeBoss;
        private void Awake()
        {
            if (bossContainer != null)
            {
                expandedSize = bossContainer.sizeDelta;
                collapsedSize = new Vector2(expandedSize.x, 0f);
                bossContainer.sizeDelta = collapsedSize;
            }

            if (bossCanvasGroup != null)
            {
                bossCanvasGroup.alpha = 0f;
            }
        }

        private void OnEnable()
        {
            EventBus.Bind<FEB_Unit_Damaged>(UnitDamaged);
            EventBus.Bind<FEB_Unit_Death>(UnitDeath);
            EventBus.Bind<FEB_Unit_AssignBossBar>(RegisterBoss);
        }

        private void OnDisable()
        {
            EventBus.Release<FEB_Unit_Damaged>(UnitDamaged);
            EventBus.Release<FEB_Unit_Death>(UnitDeath);
            EventBus.Release<FEB_Unit_AssignBossBar>(RegisterBoss);
        }

        private void Update()
        {
            foreach (var item in damageTable.Values)
            {
                if (item == null)
                    continue;

                item.TickEMA();
            }

            CleanupDeadUnits();
            UpdateBar();
            AnimateUI();
        }
    }
}