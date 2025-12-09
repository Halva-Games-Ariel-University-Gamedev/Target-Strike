using UnityEngine;

public class MenuButtons : MonoBehaviour
{
    public int missionIndex = 0;

    void Awake()
    {
        Debug.Log("awaked");

        // Force the cursor to be usable in the menu
        if (Cursor.lockState != CursorLockMode.None)
            Cursor.lockState = CursorLockMode.None;

        if (!Cursor.visible)
            Cursor.visible = true;

        if (Time.timeScale != 1f)
            Time.timeScale = 1f;
    }

    public void OnClick()
    {
        if (MissionsMenu.Instance != null)
        {
            MissionsMenu.Instance.StartMission(missionIndex);
        }
        else
        {
            Debug.LogWarning("MissionsMenu.Instance is null – did the singleton get created?");
        }
    }
}
