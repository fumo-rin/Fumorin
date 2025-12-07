using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

namespace Fumorin
{
    public class RebindsLoader : MonoBehaviour
    {
        [SerializeField] List<InputActionReference> inputActions = new();

        private void Awake()
        {
            LoadAllBindings();
        }

        public void LoadAllBindings()
        {
            foreach (var inputRef in inputActions)
            {
                if (inputRef == null || inputRef.action == null)
                {
                    Debug.LogWarning("Null InputActionReference found in RebindsLoader.");
                    continue;
                }

                string rebinds = PlayerPrefs.GetString(inputRef.action.name, string.Empty);
                if (!string.IsNullOrEmpty(rebinds))
                {
                    try
                    {
                        inputRef.action.LoadBindingOverridesFromJson(rebinds);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"Failed to load bindings for '{inputRef.action.name}': {e.Message}");
                    }
                }

                inputRef.action.Enable();
            }
        }
    }
}
