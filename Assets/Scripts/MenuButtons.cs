using System;
using System.Threading.Tasks;
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
        _OnClick();
    }

    private async void _OnClick()
    {
        try
        {
            Debug.Log("Start Clicked");

            if (MissionsMenu.Instance == null)
            {
                Debug.LogWarning("MissionsMenu.Instance is null");
                return;
            }

            Debug.Log("Before PeekAsync");
            var data = await SceneReturn.PeekAsync();
            Debug.Log($"After PeekAsync: found={data.found} scene={data.sceneName} index={data.missionIndex}");

            Debug.Log("Before StartMission");

            await MissionsMenu.Instance.StartMission(data.missionIndex >= 0 ? data.missionIndex : 0);

            Debug.Log("After StartMission call");
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }
}
