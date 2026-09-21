using UnityEngine;
using UnityEngine.UI;

namespace rinCore
{
    [RequireComponent(typeof(Button)), DefaultExecutionOrder(500)]
    public class UINestSelector : MonoBehaviour, IHierarchyComponentColor
    {
        [SerializeField, UINestID] private string nest;

        private Button b;

        public Color LabelColor => UINest.IsValidNestID(nest) ? ColorHelper.PastelGreen.Opacity(50) : ColorHelper.HierarchyTypes.Error;

        private void Awake()
        {
            b = GetComponent<Button>();
            UINest foundNest = UINest.GetNestByID(nest);
            if (foundNest == null)
                return;
            b.BindSingleAction(() => new FEB_UI_SelectNest(nest, UINest.DEFAULT_FADE_DURATION).Publish());
        }
    }
}
