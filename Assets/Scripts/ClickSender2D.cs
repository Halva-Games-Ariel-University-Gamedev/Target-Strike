using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class ClickSender2D : NetworkBehaviour
{
    Camera cam;

    void Start()
    {
        if (!IsOwner) return;
        cam = GetComponentInChildren<Camera>();
    }

    void Update()
    {
        if (!IsOwner) return;
        if (Mouse.current == null) return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 screen = Mouse.current.position.ReadValue();
            Vector3 world = cam.ScreenToWorldPoint(screen);
            world.z = 0f;

            SendClickServerRpc(world);
        }
    }

    [ServerRpc]
    void SendClickServerRpc(Vector3 worldPos, ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;

        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("ServerRpc: NetworkManager.Singleton is null.");
            return;
        }

        // Only host (Player1) is allowed to resolve clicks.
        if (senderId != NetworkManager.ServerClientId)
            return;

        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(senderId, out var client))
        {
            Debug.LogError($"ServerRpc: No ConnectedClient for id {senderId}");
            return;
        }

        var senderPlayer = client.PlayerObject;
        if (senderPlayer == null)
        {
            Debug.LogError($"ServerRpc: PlayerObject is null for client {senderId}. " +
                           "Check NetworkManager Default Player Prefab + spawning.");
            return;
        }

        bool win = ClickedClueBuilding(worldPos);

        if (RoundManager.Instance == null)
        {
            Debug.LogError("RoundManager.Instance is null. Add RoundManager (with NetworkObject) to NetworkScene.");
            return;
        }

        RoundManager.Instance.EndRound(win);
    }

    bool ClickedClueBuilding(Vector2 worldPos)
    {
        // Requires your buildings to have Collider2D
        Collider2D[] hits = Physics2D.OverlapPointAll(worldPos);

        foreach (var h in hits)
        {
            var marker = h.GetComponentInParent<BuildingMarker>();
            if (marker != null)
                return marker.isClue; // true => win, false => lose
        }

        return false; // clicked empty space
    }

    [ClientRpc]
    void ShowResultClientRpc(bool win)
    {
        ResultUI.Show(win);
    }
}
