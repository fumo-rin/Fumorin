using UnityEngine;

namespace RinCore
{
    public class TestLoadDialogue : MonoBehaviour
    {
        [SerializeField] DialogueStackSO toLoad;
        private void Start()
        {
            toLoad.StartDialogue(out _, null);
        }
    }
}
