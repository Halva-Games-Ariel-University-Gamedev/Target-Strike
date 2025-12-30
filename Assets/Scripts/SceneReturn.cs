using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Services.Authentication;
using Unity.Services.Core;
using System.Threading.Tasks;
using System.Collections.Generic;
using Unity.Services.CloudSave;

public static class SceneReturn
{
    public const string KScene = "return_scene";
    public const string KIndex = "mission_index";

    static async Task EnsureSignedInAsync()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
            await UnityServices.InitializeAsync();

        // IMPORTANT: Cloud Save needs a signed-in player.
        // If you want this to work even before account creation, sign in as guest first.
        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    // Store the intended destination in the cloud (no scene loading here)
    public static async Task SetAsync(string sceneName, int missionIndex)
    {
        await EnsureSignedInAsync();

        var save = new Dictionary<string, object>
        {
            [KScene] = sceneName,
            [KIndex] = missionIndex
        };

        await CloudSaveService.Instance.Data.Player.SaveAsync(save);
    }

    // Read (does NOT delete)
    public static async Task<(bool found, string sceneName, int missionIndex)> PeekAsync()
    {
        await EnsureSignedInAsync();

        var keys = new HashSet<string> { KScene, KIndex };
        var data = await CloudSaveService.Instance.Data.Player.LoadAsync(keys);

        if (!data.TryGetValue(KScene, out var s))
            return (false, null, -1);

        var scene = s.Value.GetAs<string>();
        var index = data.TryGetValue(KIndex, out var i) ? i.Value.GetAs<int>() : -1;

        return (true, scene, index);
    }

    // Read AND delete (one-time)
    public static async Task<(bool found, string sceneName, int missionIndex)> ConsumeAsync()
    {
        var (found, scene, index) = await PeekAsync();
        if (!found) return (false, null, -1);

        await CloudSaveService.Instance.Data.Player.DeleteAsync(KScene);
        await CloudSaveService.Instance.Data.Player.DeleteAsync(KIndex);

        return (true, scene, index);
    }
}
