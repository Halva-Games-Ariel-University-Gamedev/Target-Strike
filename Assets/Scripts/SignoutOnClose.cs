using System.Runtime.InteropServices;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;

public class SignOutOnClose : MonoBehaviour
{
    static SignOutOnClose instance;
    static bool didSignOut;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void RegisterBeforeUnload(string gameObjectName);
    [DllImport("__Internal")] private static extern void UnregisterBeforeUnload();
#endif

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

#if UNITY_WEBGL && !UNITY_EDITOR
        // Application.quitting is not reliable on WebGL; use browser unload hooks
        RegisterBeforeUnload(gameObject.name);
#endif
    }

    void OnDestroy()
    {
        if (instance == this)
        {
            Application.quitting -= ForceSignOut;

#if UNITY_WEBGL && !UNITY_EDITOR
            UnregisterBeforeUnload();
#endif
        }
    }

    // Called by the WebGL .jslib via SendMessage on tab close/refresh/navigation
    public void OnWebGLBeforeUnload()
    {
        ForceSignOut();
    }

    static void ForceSignOut()
    {
        if (didSignOut) return;
        didSignOut = true;

        Debug.Log("Sign out called");

        // If services/auth aren't initialized yet, there's nothing reliable to do.
        if (UnityServices.State != ServicesInitializationState.Initialized)
            return;

        // On unload you can't rely on network finishing; this at least clears local state.
        AuthenticationService.Instance.SignOut(true);
        AuthenticationService.Instance.ClearSessionToken();
    }
}
