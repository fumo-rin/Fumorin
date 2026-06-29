using UnityEngine;
using UnityEngine.AddressableAssets;

namespace rinCore
{
    [System.Serializable]
    public class AssetReferenceScene : AssetReference
    {
        public AssetReferenceScene(string guid) : base(guid) { }
        public override bool ValidateAsset(string path)
        {
#if UNITY_EDITOR
            var type = UnityEditor.AssetDatabase.GetMainAssetTypeAtPath(path);
            return typeof(UnityEditor.SceneAsset).IsAssignableFrom(type);
#else                        
            return false;
#endif
        }
    }

    [System.Serializable]
    public class SceneReference
    {
        [SerializeField] private AssetReferenceScene sceneReference;
        public AssetReferenceScene AddressableReference => sceneReference;
        public string GetSceneName()
        {
            if (sceneReference == null || string.IsNullOrEmpty(sceneReference.AssetGUID))
                return string.Empty;

            return sceneReference.AssetGUID;
        }

#if UNITY_EDITOR
        public UnityEditor.SceneAsset SceneAsset => sceneReference?.editorAsset as UnityEditor.SceneAsset;
        public string GetEditorSceneName()
        {
            return SceneAsset != null ? SceneAsset.name : "Missing/Unassigned Scene";
        }
#endif
    }
}