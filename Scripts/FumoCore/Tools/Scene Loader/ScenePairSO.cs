using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace rinCore
{
    [CreateAssetMenu(menuName = "Fumorin/Scene Pair")]
    public class ScenePairSO : ScriptableObject
    {
        [Header("Main scene that defines this pair")]
        [SerializeField] private SceneReference mainScene;

        [Header("Scenes that are loaded additively with the main scene")]
        [SerializeField] private List<SceneReference> additiveScenes = new();
        public List<SceneReference> Scenes
        {
            get
            {
                var list = new List<SceneReference>();
                if (mainScene != null && mainScene.IsValid)
                    list.Add(mainScene);

                list.AddRange(additiveScenes.Where(s => s != null && s.IsValid));
                return list;
            }
        }
        public SceneReference MainScene => mainScene;
        public IReadOnlyList<SceneReference> AdditiveScenes => additiveScenes;
    }
}