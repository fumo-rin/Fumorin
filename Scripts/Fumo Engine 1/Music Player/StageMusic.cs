using UnityEngine;

namespace rinCore
{
    public class StageMusic : MonoBehaviour, IHierarchyComponentColor
    {
        [SerializeField] MusicWrapper music;
        public Color LabelColor => ColorHelper.PastelYellow.Opacity(50);

        private void Start()
        {
            music.Play();
        }
    }
}
