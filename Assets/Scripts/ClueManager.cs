using Unity.Netcode;
using UnityEngine;

public class ClueManager : NetworkBehaviour
{
    public static ClueManager Instance;

    void Awake() => Instance = this;

    // called by server when P1 (host) reports
    public void ReceiveClueFromP1(Vector2 cluePos)
    {
        if (!IsServer) return;

        ulong p2Id = FindPlayer2ClientId();
        if (p2Id == ulong.MaxValue)
        {
            Debug.LogWarning("ClueManager: Player2 not connected yet.");
            return;
        }

        var rpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { p2Id } }
        };

        SendClueClientRpc(cluePos, rpcParams);
    }

    // Player2 = first non-server client
    ulong FindPlayer2ClientId()
    {
        foreach (var kv in NetworkManager.Singleton.ConnectedClientsList)
        {
            ulong id = kv.ClientId;
            if (id != NetworkManager.ServerClientId)
                return id;
        }
        return ulong.MaxValue;
    }

    [ClientRpc]
    void SendClueClientRpc(Vector2 buildingPos, ClientRpcParams rpcParams = default)
    {
        // runs only on P2 (targeted)
        ClueReceiver.ShowClue(buildingPos);
    }
}
