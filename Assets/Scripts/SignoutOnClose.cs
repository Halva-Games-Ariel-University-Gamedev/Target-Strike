using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;

public class SignOutOnClose : MonoBehaviour
{
    static SignOutOnClose instance;

    void Awake()
    {
        // Keep ONE copy across all scenes
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        DontDestroyOnLoad(gameObject);
        Application.quitting += ForceSignOut;
    }

    void OnDestroy()
    {
        if (instance == this)
            Application.quitting -= ForceSignOut;
    }

    static void ForceSignOut()
    {
        Debug.Log("Sign out called");

        if (UnityServices.State != ServicesInitializationState.Initialized)
            return;

        AuthenticationService.Instance.SignOut(true);
        AuthenticationService.Instance.ClearSessionToken();
    }
}
