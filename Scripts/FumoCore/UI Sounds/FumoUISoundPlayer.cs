using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace rinCore
{
    [DefaultExecutionOrder(-5555)]
    public class FumoUISoundManager : MonoBehaviour
    {
        public static FumoUISoundManager Instance;

        [Header("Input")]
        [SerializeField] private InputActionReference submitAction;

        [Header("UI Sounds")]
        public ACWrapper hoverSound;
        public ACWrapper clickSound;

        private GameObject lastSelectedOrHovered;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            if (submitAction != null && submitAction.action != null)
            {
                submitAction.action.Enable();
            }
        }

        private void Update()
        {
            if (EventSystem.current == null || SceneLoader.IsLoading) return;

            HandleSelectionAndHoverSound();
            HandleSubmitSound();
        }

        #region Hover & Selection Detection

        private void HandleSelectionAndHoverSound()
        {
            GameObject currentTarget = GetCurrentInteractableTarget();

            if (currentTarget != null && currentTarget != lastSelectedOrHovered)
            {
                lastSelectedOrHovered = currentTarget;
                hoverSound?.Play(ALHandler.Position);
            }
            else if (currentTarget == null)
            {
                lastSelectedOrHovered = null;
            }
        }

        private GameObject GetCurrentInteractableTarget()
        {
            GameObject selected = EventSystem.current.currentSelectedGameObject;
            if (selected != null && selected.activeInHierarchy && selected.GetComponent<Selectable>() is Selectable sel && sel.interactable)
            {
                return selected;
            }

            if (Mouse.current != null)
            {
                var pointerPos = Mouse.current.position.ReadValue();
                var eventData = new PointerEventData(EventSystem.current) { position = pointerPos };
                var results = new List<RaycastResult>();
                EventSystem.current.RaycastAll(eventData, results);

                foreach (var result in results)
                {
                    if (result.gameObject.GetComponent<Selectable>() is Selectable hoverSel && hoverSel.interactable)
                    {
                        return result.gameObject;
                    }
                }
            }

            return null;
        }

        #endregion

        #region Submit Detection

        private void HandleSubmitSound()
        {
            if (!WasSubmitPressedThisFrame()) return;

            GameObject target = GetCurrentInteractableTarget();
            if (target != null && target.activeInHierarchy)
            {
                clickSound?.Play(ALHandler.Position);
            }
        }

        private bool WasSubmitPressedThisFrame()
        {
            bool mouseClicked = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
            bool actionTriggered = submitAction != null && submitAction.action != null && submitAction.action.WasPressedThisFrame();

            return mouseClicked || actionTriggered;
        }

        #endregion
    }
}