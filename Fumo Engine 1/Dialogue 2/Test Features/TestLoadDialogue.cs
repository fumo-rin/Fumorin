using UnityEngine;

namespace Fumorin
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
