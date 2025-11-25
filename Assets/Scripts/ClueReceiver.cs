using UnityEngine;
using TMPro;

public class ClueReceiver : MonoBehaviour
{
    public static ClueReceiver Instance;

    public GameObject markerPrefab; // a small sprite for the clue
    public TMP_Text clueText;        // optional UI text on P2 screen

    GameObject currentMarker;

    void Awake() => Instance = this;

    public static void ShowClue(Vector2 pos)
    {
        if (Instance == null)
        {
            Debug.LogWarning("ClueReceiver not in CluesScene.");
            return;
        }
        Instance.Show(pos);
    }

    void Show(Vector2 pos)
    {
        if (currentMarker != null)
            Destroy(currentMarker);
        else Debug.Log("no current marker");

        if (markerPrefab != null)
            currentMarker = Instantiate(markerPrefab, pos, Quaternion.identity);
        else Debug.Log("no marker prefab");

        if (clueText != null)
            clueText.text = $"Target building at: {pos.x:0.0}, {pos.y:0.0}";
        else Debug.Log("no clue text marker");
    }

    public void Clear()
    {
        if (currentMarker != null) Destroy(currentMarker);
        currentMarker = null;

        if (clueText != null) clueText.text = "";
    }
}
