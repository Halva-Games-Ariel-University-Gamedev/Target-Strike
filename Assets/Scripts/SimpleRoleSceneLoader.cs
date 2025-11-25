using Unity.Netcode;
using UnityEngine.SceneManagement;

public class SimpleRoleSceneLoader : NetworkBehaviour
{
    public string puzzleSceneName = "PuzzleScene";
    public string cluesSceneName = "CluesScene";

    // Static guard per client so even if host has 2 player objects by accident,
    // only one will load scenes.
    static bool localLoaded;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;
        if (localLoaded) return;
        localLoaded = true;

        var nm = NetworkManager.Singleton;

        string toLoad = (nm != null && nm.IsHost) ? puzzleSceneName : cluesSceneName;
        // string toLoad = (nm != null && nm.IsHost) ? cluesSceneName : puzzleSceneName;
        LoadOnce(toLoad);
    }

    void LoadOnce(string sceneName)
    {
        if (SceneManager.GetSceneByName(sceneName).isLoaded)
            return;

        SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner)
            localLoaded = false; // lets you reconnect in editor
    }
}
