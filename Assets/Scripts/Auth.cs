using System.Threading.Tasks;
using Unity.Services.Core;
using Unity.Services.Authentication;
using UnityEngine;

public static class Auth
{
    public static async Task InitAsync()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
            await UnityServices.InitializeAsync();
    }

    // If a session token exists, SignInAnonymouslyAsync recovers the cached player.
    public static async Task<bool> TryAutoSignInAsync()
    {
        if (!AuthenticationService.Instance.SessionTokenExists) return false;
        //await AuthenticationService.Instance.SignInAnonymouslyAsync();
        //return true;
        return false;
    }

    public static Task SignUpAsync(string username, string password) =>
        AuthenticationService.Instance.SignUpWithUsernamePasswordAsync(username, password);

    public static Task SignInAsync(string username, string password) =>
        AuthenticationService.Instance.SignInWithUsernamePasswordAsync(username, password);

    // Use this to KEEP the same player (and their Cloud Save) and “upgrade” anon -> username/password.
    public static Task LinkToUsernamePasswordAsync(string username, string password) => null;
    //.Instance.AddUsernamePasswordAsync(username, password);

    public static void SignOutAndForgetDeviceToken()
    {
        AuthenticationService.Instance.SignOut(true);
        AuthenticationService.Instance.ClearSessionToken(); // clearing can permanently lose an unlinked anon account
    }
}
