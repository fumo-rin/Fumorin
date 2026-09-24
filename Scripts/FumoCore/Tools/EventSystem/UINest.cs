using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace rinCore
{
    #region Editor Dropdown ID
    public class UINestIDAttribute : PropertyAttribute { }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(UINestIDAttribute))]
    public class UINestIDDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            UINest[] nests = UnityEngine.Object.FindObjectsByType<UINest>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            List<string> nestIDs = new List<string>();

            foreach (var nest in nests)
            {
                if (nest != null && !string.IsNullOrEmpty(nest.NestID) && !nestIDs.Contains(nest.NestID))
                {
                    nestIDs.Add(nest.NestID);
                }
            }

            string currentVal = property.stringValue;
            bool isValid = !string.IsNullOrEmpty(currentVal) && nestIDs.Contains(currentVal);

            if (nestIDs.Count == 0)
            {
                EditorGUI.BeginChangeCheck();
                string newVal = EditorGUI.TextField(position, label, currentVal);
                if (EditorGUI.EndChangeCheck())
                {
                    property.stringValue = newVal;
                }
                return;
            }

            List<string> displayOptions = new List<string>(nestIDs);
            if (!isValid)
            {
                if (string.IsNullOrEmpty(currentVal))
                {
                    displayOptions.Insert(0, "<None / Empty>");
                }
                else
                {
                    displayOptions.Insert(0, $"{currentVal} [MISSING]");
                }
            }

            int currentIndex = isValid ? displayOptions.IndexOf(currentVal) : 0;

            EditorGUI.BeginChangeCheck();
            int selectedIndex = EditorGUI.Popup(position, label.text, currentIndex, displayOptions.ToArray());

            if (EditorGUI.EndChangeCheck() && selectedIndex >= 0 && selectedIndex < displayOptions.Count)
            {
                string selectedStr = displayOptions[selectedIndex];

                if (!selectedStr.EndsWith("[MISSING]") && selectedStr != "<None / Empty>")
                {
                    property.stringValue = selectedStr;
                }
                else if (selectedStr == "<None / Empty>")
                {
                    property.stringValue = string.Empty;
                }
            }
        }
    }
#endif
    #endregion

    public interface IUINestRunable
    {
        public int RunnerPriority { get; }
        void RunNestComponent(UINest nest);
    }

    #region Events
    public record FEB_UI_SelectNest(string TargetNestID, float Duration) : IRinEvent;
    public record FEB_UI_ClearSelection(string TargetNestID, float FadeOutDuration, bool DisregardNestCloseable) : IRinEvent;
    #endregion
    #region State & Transitions
    public partial class UINest
    {
        public void TransitionTo(UINest next, float duration = -1f)
        {
            if (next == this) return;

            float actualDuration = duration < 0f ? DEFAULT_FADE_DURATION : duration;

            if (sequenceRoutine != null)
            {
                StopCoroutine(sequenceRoutine);
                sequenceRoutine = null;
            }

            sequenceRoutine = StartCoroutine(CO_SequentialTransition(this, next, actualDuration));
        }

        private static IEnumerator CO_SequentialTransition(List<UINest> currentActiveNests, UINest target, float totalDuration)
        {
            isGlobalTransitioning = true;

            bool hasActiveNests = currentActiveNests != null && currentActiveNests.Count > 0;
            float phaseDuration = (hasActiveNests && target != null) ? totalDuration * 0.5f : totalDuration;

            if (hasActiveNests && phaseDuration > 0f)
            {
                int completedFades = 0;
                int totalFades = currentActiveNests.Count;

                foreach (var current in currentActiveNests)
                {
                    if (current != null)
                    {
                        current.FadeOut(phaseDuration, () => completedFades++);
                    }
                    else
                    {
                        completedFades++;
                    }
                }

                float timeout = phaseDuration + 0.1f;
                float elapsed = 0f;

                while (completedFades < totalFades && elapsed < timeout)
                {
                    elapsed += Time.unscaledDeltaTime;
                    yield return null;
                }
            }

            if (hasActiveNests)
            {
                foreach (var current in currentActiveNests)
                {
                    if (current != null)
                    {
                        current.SetStateDirect(false);
                    }
                }
            }

            if (target != null && target.cGroup != null)
            {
                if (!target.cGroup.gameObject.activeSelf)
                {
                    target.cGroup.gameObject.SetActive(true);
                }

                bool fadeInComplete = false;
                target.FadeIn(phaseDuration, () => fadeInComplete = true);

                if (phaseDuration > 0f)
                {
                    float timeout = phaseDuration + 0.1f;
                    float elapsed = 0f;

                    while (!fadeInComplete && elapsed < timeout)
                    {
                        if (target == null || target.cGroup == null || !target.cGroup.gameObject.activeInHierarchy) break;
                        elapsed += Time.unscaledDeltaTime;
                        yield return null;
                    }
                }
            }
            else
            {
                if (EventSystem.current != null)
                {
                    EventSystem.current.SetSelectedGameObject(null);
                }
            }

            isGlobalTransitioning = false;
        }

        private static IEnumerator CO_SequentialTransition(UINest current, UINest target, float totalDuration)
        {
            List<UINest> currentList = null;
            if (current != null)
            {
                currentList = new List<UINest> { current };
            }
            yield return CO_SequentialTransition(currentList, target, totalDuration);
        }

        public void FadeIn(float duration, Action onComplete = null)
        {
            StartTransition(1f, true, duration, onComplete);
        }

        public void FadeOut(float duration, Action onComplete = null)
        {
            StartTransition(0f, false, duration, onComplete);
        }

        private void StartTransition(float targetAlpha, bool interactable, float duration, Action onComplete = null)
        {
            if (activeTransitionRoutine != null)
            {
                StopCoroutine(activeTransitionRoutine);
                activeTransitionRoutine = null;
            }

            activeTransitionRoutine = StartCoroutine(CO_Transition(targetAlpha, interactable, duration, onComplete));
        }

        private IEnumerator CO_Transition(float targetAlpha, bool interactable, float duration, Action onComplete = null)
        {
            if (cGroup == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            if (duration <= 0f)
            {
                SetStateDirect(interactable, forceExecution: true);
                activeTransitionRoutine = null;
                onComplete?.Invoke();
                yield break;
            }

            cGroup.blocksRaycasts = false;
            cGroup.interactable = false;

            float startAlpha = cGroup.alpha;
            bool isFadingIn = targetAlpha > startAlpha;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float normalizedTime = Mathf.Clamp01(elapsed / duration);

                float curveVal = isFadingIn
                    ? LerpCurves.EaseInCubic(normalizedTime)
                    : 1f - LerpCurves.EaseInCubic(1f - normalizedTime);

                cGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, curveVal);
                yield return null;
            }

            SetStateDirect(interactable, forceExecution: true);

            activeTransitionRoutine = null;
            onComplete?.Invoke();
        }

        public void SetStateDirect(bool active, bool forceExecution = false)
        {
            if (cGroup == null) return;

            bool stateUnchanged = (cGroup.interactable == active) && (cGroup.alpha == (active ? 1f : 0f));

            if (stateUnchanged && !forceExecution)
            {
                return;
            }

            cGroup.alpha = active ? 1f : 0f;
            cGroup.blocksRaycasts = active;
            cGroup.interactable = active;

            if (active)
            {
                if (defaultSelection != null)
                {
                    new FEB_EventSystem_SelectBuffered(defaultSelection).Publish();
                }
                ExecuteRunners();
            }
        }
    }
    #endregion
    #region Execution & Selection
    public partial class UINest
    {
        public void CacheRunners()
        {
            if (cGroup == null)
            {
                nestRunners = new List<IUINestRunable>();
                return;
            }

            Canvas rootCanvas = cGroup.GetComponentInParent<Canvas>();
            Transform searchRoot = rootCanvas != null ? rootCanvas.transform : cGroup.transform.root;

            nestRunners = searchRoot.GetComponentsInChildren<IUINestRunable>(true)
                .Where(r => r != null)
                .OrderByDescending(r => r.RunnerPriority)
                .ToList();
        }

        public void ExecuteRunners()
        {
            if (nestRunners == null || nestRunners.Count == 0 || nestRunners.Any(r => r == null))
            {
                CacheRunners();
            }

            for (int i = 0; i < nestRunners.Count; i++)
            {
                nestRunners[i]?.RunNestComponent(this);
            }
        }
    }
    #endregion

    #region Event Bus Handling
    public partial class UINest
    {
        private void HandleNestChangeRequest(FEB_UI_SelectNest evt)
        {
            if (isGlobalTransitioning) return;
            if (UINest.GetNestByID(evt.TargetNestID) == null)
                return;

            bool isTarget = !string.IsNullOrEmpty(evt.TargetNestID) && evt.TargetNestID == nestID;
            float duration = evt.Duration < 0f ? DEFAULT_FADE_DURATION : evt.Duration;

            if (isTarget)
            {
                List<UINest> currentlyVisibleNests = activeNests
                    .Where(n => n != null && n != this && n.cGroup != null && n.cGroup.alpha > 0f)
                    .ToList();

                if (sequenceRoutine != null)
                {
                    StopCoroutine(sequenceRoutine);
                    sequenceRoutine = null;
                }

                sequenceRoutine = StartCoroutine(CO_SequentialTransition(currentlyVisibleNests, this, duration));
            }
            else if (!IsValidNestID(evt.TargetNestID))
            {
                List<UINest> currentlyVisibleNests = activeNests
                    .Where(n => n != null && n.cGroup != null && n.cGroup.alpha > 0f)
                    .ToList();

                if (currentlyVisibleNests.Count > 0)
                {
                    UINest runner = currentlyVisibleNests.FirstOrDefault(n => n == this) ?? currentlyVisibleNests[0];
                    if (runner == this)
                    {
                        if (sequenceRoutine != null)
                        {
                            StopCoroutine(sequenceRoutine);
                            sequenceRoutine = null;
                        }

                        sequenceRoutine = StartCoroutine(CO_SequentialTransition(currentlyVisibleNests, null, duration));
                    }
                }
            }
        }

        private void HandleClearSelectionRequest(FEB_UI_ClearSelection evt)
        {
            if (isGlobalTransitioning) return;

            bool hasSpecificTarget = !string.IsNullOrEmpty(evt.TargetNestID);
            if (hasSpecificTarget && evt.TargetNestID != nestID) return;
            if (hasSpecificTarget && !evt.DisregardNestCloseable && !isCloseable) return;

            if (cGroup != null && cGroup.alpha > 0f)
            {
                List<UINest> nestsToClose;

                if (hasSpecificTarget)
                {
                    nestsToClose = new List<UINest> { this };
                }
                else
                {
                    nestsToClose = activeNests
                        .Where(n => n != null && n.cGroup != null && n.cGroup.alpha > 0f && (evt.DisregardNestCloseable || n.isCloseable))
                        .ToList();
                }

                if (nestsToClose.Count > 0)
                {
                    UINest runner = nestsToClose.FirstOrDefault(n => n == this) ?? nestsToClose[0];
                    if (runner == this)
                    {
                        if (sequenceRoutine != null)
                        {
                            StopCoroutine(sequenceRoutine);
                            sequenceRoutine = null;
                        }

                        sequenceRoutine = StartCoroutine(CO_SequentialTransition(nestsToClose, null, evt.FadeOutDuration));
                    }
                }
            }
        }
    }
    #endregion

    #region Static Utilities & Registry
    public partial class UINest
    {
        private static readonly HashSet<UINest> activeNests = new();
        public static IEnumerable<UINest> ActiveNests => activeNests.Where(n => n != null).OrderBy(x => -x.Priority);
        private static bool isGlobalTransitioning = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            activeNests.Clear();
            isGlobalTransitioning = false;
        }

        public static bool IsValidNestID(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UINest[] sceneNests = UnityEngine.Object.FindObjectsByType<UINest>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                return sceneNests.Any(n => n != null && n.nestID == id);
            }
#endif
            return GetNestByID(id) != null;
        }

        public static UINest GetNestByID(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            foreach (var nest in activeNests)
            {
                if (nest != null && nest.nestID == id) return nest;
            }
            return null;
        }
    }
    #endregion

    public partial class UINest : MonoBehaviour, IHierarchyComponentColor
    {
        [Header("Components")]
        [SerializeField] private CanvasGroup cGroup;

        [Header("Nest Identity")]
        [SerializeField] private string nestID = "MainMenu";
        [SerializeField] private int priority = 0;
        [SerializeField] private bool isCloseable = false;
        public const float DEFAULT_FADE_DURATION = 0.35f;
        [SerializeField] private GameObject defaultSelection;

        public CanvasGroup canvasGroup => cGroup;
        public string NestID => nestID;
        public int Priority => priority;
        public bool IsCloseable => isCloseable;

        public Color LabelColor => cGroup != null ? ColorHelper.HierarchyTypes.UINest : ColorHelper.HierarchyTypes.Error;

        private Coroutine activeTransitionRoutine;
        private Coroutine sequenceRoutine;
        private Coroutine selectionRoutine;
        private List<IUINestRunable> nestRunners = new();
        private GameObject lastValidSelection;

        private void Awake()
        {
            CacheRunners();
        }

        private void OnEnable()
        {
            activeNests.Add(this);
            EventBus.Bind<FEB_UI_SelectNest>(HandleNestChangeRequest);
            EventBus.Bind<FEB_UI_ClearSelection>(HandleClearSelectionRequest);
        }

        private void OnDisable()
        {
            activeNests.Remove(this);
            EventBus.Release<FEB_UI_SelectNest>(HandleNestChangeRequest);
            EventBus.Release<FEB_UI_ClearSelection>(HandleClearSelectionRequest);

            StopAllCoroutines();
            activeTransitionRoutine = null;
            sequenceRoutine = null;
            selectionRoutine = null;

            if (activeNests.Count == 0 || activeNests.All(n => n.sequenceRoutine == null))
            {
                isGlobalTransitioning = false;
            }
        }

        private void OnDestroy()
        {
            activeNests.Remove(this);
            activeNests.RemoveWhere(n => n == null);
        }

        private void Start()
        {
            CacheRunners();

            int maxPriority = int.MinValue;
            bool foundValidCandidate = false;

            foreach (var nest in activeNests)
            {
                if (nest != null && nest.priority >= 0)
                {
                    foundValidCandidate = true;
                    if (nest.priority > maxPriority)
                    {
                        maxPriority = nest.priority;
                    }
                }
            }

            bool isHighestPriority = foundValidCandidate && priority >= 0 && priority == maxPriority;
            SetStateDirect(isHighestPriority, forceExecution: isHighestPriority);
        }
    }
}