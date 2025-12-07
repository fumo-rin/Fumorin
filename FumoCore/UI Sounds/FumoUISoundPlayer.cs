using FumoCore.Input;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace FumoCore.Tools
{
    [DefaultExecutionOrder(-5555)]
    public class FumoUISoundManager : MonoBehaviour
    {
        public static FumoUISoundManager Instance;

        [Header("Input")]
        [SerializeField] InputActionReference submitAction;

        [Header("UI Sounds")]
        public ACWrapper hoverSound;
        public ACWrapper clickSound;

        private GameObject lastHovered;
        private bool submitPressedLastFrame;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            if (EventSystem.current == null) return;

            HandleHover();
            HandleSubmit();
        }

        #region Hover Detection

        private void HandleHover()
        {
            GameObject hoverTarget = null;

            if (Mouse.current != null)
            {
                var pointerPos = Mouse.current.position.ReadValue();
                var eventData = new PointerEventData(EventSystem.current) { position = pointerPos };
                var results = new System.Collections.Generic.List<RaycastResult>();
                EventSystem.current.RaycastAll(eventData, results);

                foreach (var result in results)
                {
                    if (result.gameObject.GetComponent<Selectable>() is Selectable s && s.interactable)
                    {
                        hoverTarget = result.gameObject;
                        break;
                    }
                }
            }

            if (hoverTarget == null)
            {
                var selected = EventSystem.current.currentSelectedGameObject;
                if (selected != null && selected.GetComponent<Selectable>() is Selectable s && s.interactable)
                    hoverTarget = selected;
            }

            if (hoverTarget != lastHovered && hoverTarget != null)
            {
                lastHovered = hoverTarget;
                hoverSound?.Play(ALHandler.Position);
            }

            lastHovered = hoverTarget;
        }

        #endregion

        #region Submit Detection

        private void HandleSubmit()
        {
            bool submitPressed = IsSubmitPressed();

            GameObject submitTarget = null;

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                var pointerPos = Mouse.current.position.ReadValue();
                var eventData = new PointerEventData(EventSystem.current) { position = pointerPos };
                var results = new System.Collections.Generic.List<RaycastResult>();
                EventSystem.current.RaycastAll(eventData, results);

                foreach (var result in results)
                {
                    if (result.gameObject.GetComponent<Selectable>() is Selectable s && s.interactable)
                    {
                        submitTarget = result.gameObject;
                        break;
                    }
                }
            }

            if (submitTarget == null && submitAction != null && submitAction.action != null && submitAction.action.WasPressedThisFrame())
            {
                var selected = EventSystem.current.currentSelectedGameObject;
                if (selected != null && selected.GetComponent<Selectable>() is Selectable s && s.interactable)
                    submitTarget = selected;
            }

            if (submitPressed && !submitPressedLastFrame && submitTarget != null && submitTarget.activeInHierarchy)
            {
                clickSound?.Play(ALHandler.Position);
            }

            submitPressedLastFrame = submitPressed;
        }

        private bool IsSubmitPressed()
        {
            bool mousePressed = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
            bool gamepadPressed = submitAction != null && submitAction.action != null && submitAction.action.WasPressedThisFrame();
            return mousePressed || gamepadPressed;
        }

        #endregion
    }
}
