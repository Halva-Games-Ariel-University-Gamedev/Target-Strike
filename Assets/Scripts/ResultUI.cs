using TMPro;
using UnityEngine;

public class ResultUI : MonoBehaviour
{
    public static ResultUI Instance;
    public TMP_Text resultText;

    void Awake() => Instance = this;

    public static void Show(bool win)
    {
        if (Instance == null || Instance.resultText == null)
        {
            Debug.LogWarning("ResultUI missing in this scene.");
            return;
        }

        Instance.resultText.gameObject.SetActive(true);
        Instance.resultText.text = win ? "You win!" : "You Lose :(";
    }

    public static void Hide()
    {
        if (Instance == null || Instance.resultText == null) return;
        Instance.resultText.gameObject.SetActive(false);
    }
}
