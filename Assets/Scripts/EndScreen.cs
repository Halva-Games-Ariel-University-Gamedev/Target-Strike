using System;
using Unity.VisualScripting;
using UnityEngine;

public class EndScreen : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    bool activated = false;

    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0) && !activated) // 0 = left click
        {
            activated = true;
            _OnStartClick();
        }
    }

    private async void _OnStartClick()
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

            await MissionsMenu.Instance.StartMission(data.missionIndex >= 0 ? data.missionIndex : 0, true);

            Debug.Log("After StartMission call");
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

}
