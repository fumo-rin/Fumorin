using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshRenderer))]
public class MeshSortingOrder : MonoBehaviour
{
    [SerializeField]
    private string sortingLayerName = "Default";

    [SerializeField]
    private int sortingOrder = 0;

    private MeshRenderer meshRenderer;

    private void OnValidate()
    {
        UpdateSorting();
    }

    private void Awake()
    {
        UpdateSorting();
    }

    public void UpdateSorting()
    {
        if (meshRenderer == null)
            meshRenderer = GetComponent<MeshRenderer>();

        if (meshRenderer != null)
        {
            meshRenderer.sortingLayerName = sortingLayerName;
            meshRenderer.sortingOrder = sortingOrder;
        }
    }
}