using UnityEngine;

public class BuildingInfo : MonoBehaviour
{
    public int id;
    public bool isTarget;

    public int gridX;
    public int gridZ;

    [Header("Outline")]
    public GameObject outlineMesh; // drag OutlineMesh here in Inspector

    private void Awake()
    {

    }

    public void SetHighlighted(bool value)
    {
        if (outlineMesh != null)
            outlineMesh.SetActive(value);
    }
}
