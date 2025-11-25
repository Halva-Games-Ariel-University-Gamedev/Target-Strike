using System.Threading.Tasks;
using UnityEngine;

using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

using Unity.Services.Core;
using Unity.Services.Authentication;

using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Networking.Transport.Relay; // <- for RelayServerData

public class RelayManager : MonoBehaviour
{
    public static RelayManager Instance;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public async Task InitUGS()
    {
        // Initialize UGS once
        if (UnityServices.State == ServicesInitializationState.Uninitialized)
            await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    // Host creates allocation + join code, then starts NGO host
    public async Task<string> CreateRelayAndHost(int maxPlayers = 2)
    {
        await InitUGS();

        // maxPlayers includes host; allocation wants "connections" (others)
        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers - 1);
        string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

        // WebGL must use WSS; others can use DTLS (encrypted UDP)
        string connType = GetConnectionType();

        // NEW: use RelayServerData directly (no AllocationUtils)
        var relayServerData = new RelayServerData(allocation, connType);
        transport.SetRelayServerData(relayServerData);

        NetworkManager.Singleton.StartHost();
        return joinCode;
    }

    // Client joins allocation via code then starts NGO client
    public async Task JoinRelayAndClient(string joinCode)
    {
        await InitUGS();

        JoinAllocation joinAlloc = await RelayService.Instance.JoinAllocationAsync(joinCode);

        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

        string connType = GetConnectionType();

        // NEW: use RelayServerData directly (no AllocationUtils)
        var relayServerData = new RelayServerData(joinAlloc, connType);
        transport.SetRelayServerData(relayServerData);

        NetworkManager.Singleton.StartClient();
    }

    string GetConnectionType()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return "wss";   // WebSockets on WebGL
#else
        return "dtls";  // UDP/DTLS elsewhere
#endif
    }
}
