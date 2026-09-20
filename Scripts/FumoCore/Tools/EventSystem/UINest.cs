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
    public interface IUINestRunable
    {
        public int RunnerPriority { get; }
        void RunNestComponent(UINest nest);
    }

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
                if (!string.IsNullOrEmpty(nest.NestID) && !nestIDs.Contains(nest.NestID))
                {
                    nestIDs.Add(nest.NestID);
                }
            }

            string currentVal = property.stringValue;
            bool isValid = !string.IsNullOrEmpty(currentVal) && nestIDs.Contains(currentVal);

            if (nestIDs.Count == 0)
            {
                Rect labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, position.height);
                Rect fieldRect = new Rect(position.x + EditorGUIUtility.labelWidth, position.width - EditorGUIUtility.labelWidth, position.height, position.height);

                EditorGUI.LabelField(labelRect, label);
                property.stringValue = EditorGUI.TextField(fieldRect, currentVal);
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
            int selectedIndex = EditorGUI.Popup(position, label.text, currentIndex, displayOptions.ToArray());

            if (selectedIndex >= 0 && selectedIndex < displayOptions.Count)
            {
                string selectedStr = displayOptions[selectedIndex];

                if (!selectedStr.EndsWith("[MISSING]") && selectedStr != "<None / Empty>")
                {
                    property.stringValue = selectedStr;
                }
            }
        }
    }
#endif

    public struct FEB_UI_SelectNest : IRinEvent
    {
        public string TargetNestID;
        public float Duration;

        public FEB_UI_SelectNest(string targetNestID, float duration = -1f)
        {
            TargetNestID = targetNestID;
            Duration = duration;
        }

        public readonly bool IsValid => UINest.IsValidNestID(TargetNestID);
    }

    public class UINest : MonoBehaviour, IHierarchyComponentColor
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

        [Header("Components")]
        [SerializeField] private CanvasGroup cGroup;

        [Header("Nest Identity")]
        [SerializeField] private string nestID = "MainMenu";
        [SerializeField] private int priority = 0;
        public const float DEFAULT_FADE_DURATION = 0.35f;
        [SerializeField] private GameObject defaultSelection;

        public CanvasGroup canvasGroup => cGroup;
        public string NestID => nestID;
        public int Priority => priority;

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

        private void OnEnable()
        {
            activeNests.Add(this);
            EventBus.Bind<FEB_UI_SelectNest>(HandleNestChangeRequest);
        }

        private void OnDisable()
        {
            activeNests.Remove(this);
            EventBus.Release<FEB_UI_SelectNest>(HandleNestChangeRequest);

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
            foreach (var nest in activeNests)
            {
                if (nest != null && nest.priority > maxPriority)
                {
                    maxPriority = nest.priority;
                }
            }

            bool isHighestPriority = priority == maxPriority;
            SetStateDirect(isHighestPriority);
        }

        public void TransitionTo(UINest next, float duration = -1f)
        {
            if (next == this) return;

            float actualDuration = duration < 0f ? DEFAULT_FADE_DURATION : duration;

            if (next == null)
            {
                FadeOut(actualDuration);
                return;
            }

            if (sequenceRoutine != null)
            {
                StopCoroutine(sequenceRoutine);
                sequenceRoutine = null;
            }

            sequenceRoutine = StartCoroutine(CO_SequentialTransition(this, next, actualDuration));
        }

        private static IEnumerator CO_SequentialTransition(UINest current, UINest target, float totalDuration)
        {
            isGlobalTransitioning = true;

            bool hasActiveCurrent = current != null && current.cGroup != null && current.cGroup.alpha > 0f;
            float phaseDuration = (hasActiveCurrent && target != null) ? totalDuration * 0.5f : totalDuration;

            if (hasActiveCurrent && phaseDuration > 0f)
            {
                bool fadeOutComplete = false;
                current.FadeOut(phaseDuration, () => fadeOutComplete = true);

                float timeout = phaseDuration + 0.1f;
                float elapsed = 0f;

                while (!fadeOutComplete && elapsed < timeout)
                {
                    if (current == null || current.cGroup == null || !current.cGroup.gameObject.activeInHierarchy) break;
                    elapsed += Time.unscaledDeltaTime;
                    yield return null;
                }
            }

            if (current != null)
            {
                current.SetStateDirect(false);
            }

            if (target != null && target.cGroup != null)
            {
                if (!target.cGroup.gameObject.activeSelf)
                {
                    target.cGroup.gameObject.SetActive(true);
                }

                bool fadeInComplete = false;
                target.FadeIn(phaseDuration, () => fadeInComplete = true);

                float timeout = phaseDuration + 0.1f;
                float elapsed = 0f;

                while (!fadeInComplete && elapsed < timeout)
                {
                    if (target == null || target.cGroup == null || !target.cGroup.gameObject.activeInHierarchy) break;
                    elapsed += Time.unscaledDeltaTime;
                    yield return null;
                }

                target.SetStateDirect(true);
            }

            isGlobalTransitioning = false;
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
                SetStateDirect(interactable);
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

            cGroup.alpha = targetAlpha;

            if (interactable)
            {
                cGroup.blocksRaycasts = true;
                cGroup.interactable = true;
                ExecuteRunners();
                RequestSelection(defaultSelection);
            }

            activeTransitionRoutine = null;
            onComplete?.Invoke();
        }

        public void SetStateDirect(bool active)
        {
            if (cGroup == null) return;

            cGroup.alpha = active ? 1f : 0f;
            cGroup.blocksRaycasts = active;
            cGroup.interactable = active;

            if (active)
            {
                ExecuteRunners();
                RequestSelection(defaultSelection);
            }
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

        private void RequestSelection(GameObject target)
        {
            if (target == null) return;

            if (selectionRoutine != null)
            {
                StopCoroutine(selectionRoutine);
            }

            selectionRoutine = StartCoroutine(CO_ApplySelection(target));
        }

        private IEnumerator CO_ApplySelection(GameObject target)
        {
            yield return null;

            if (cGroup != null && cGroup.interactable && target != null && target.activeInHierarchy)
            {
                if (EventSystem.current != null)
                {
                    EventSystem.current.SetSelectedGameObject(null);
                    EventSystem.current.SetSelectedGameObject(target);
                }

                new FEB_EventSystem_SelectBuffered(target).Publish();
                lastValidSelection = target;
            }

            selectionRoutine = null;
        }

        private void HandleNestChangeRequest(FEB_UI_SelectNest evt)
        {
            if (evt.TargetNestID != nestID || isGlobalTransitioning) return;

            float duration = evt.Duration < 0f ? DEFAULT_FADE_DURATION : evt.Duration;

            UINest activeNest = activeNests.FirstOrDefault(n => n != null && n != this && n.cGroup != null && n.cGroup.alpha > 0f);

            if (sequenceRoutine != null)
            {
                StopCoroutine(sequenceRoutine);
                sequenceRoutine = null;
            }

            sequenceRoutine = StartCoroutine(CO_SequentialTransition(activeNest, this, duration));
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
}