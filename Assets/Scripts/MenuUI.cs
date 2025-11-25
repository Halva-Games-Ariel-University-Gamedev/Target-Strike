using UnityEngine;
using TMPro;
using Unity.Netcode;

public class MenuUI : MonoBehaviour
{
    public TMP_InputField joinCodeInput;
    public TMP_Text joinCodeLabel;

    [Header("UI Root To Hide")]
    public GameObject menuRoot; // drag your Canvas (or a panel) here

    async void Start()
    {
        if (RelayManager.Instance == null)
        {
            Debug.LogError("RelayManager missing in scene.");
            return;
        }

        await RelayManager.Instance.InitUGS();

        // If you want auto-hide when a connection happens:
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
    }

    public async void Host()
    {
        string code = await RelayManager.Instance.CreateRelayAndHost(2);

        if (joinCodeLabel != null)
            joinCodeLabel.text = $"Join Code: {code}";
        else
            Debug.Log("Join Code: " + code);

        HideMenu(); // host is now in game
    }

    public async void Join()
    {
        string code = joinCodeInput.text.Trim();
        if (string.IsNullOrEmpty(code)) return;

        await RelayManager.Instance.JoinRelayAndClient(code);

        // client will hide when connected, but you can hide immediately too:
        HideMenu();
    }

    void OnClientConnected(ulong clientId)
    {
        // Only hide for *this* local client (not for everyone)
        if (clientId == NetworkManager.Singleton.LocalClientId)
            HideMenu();
    }

    void OnClientDisconnected(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
            ShowMenu();
    }

    void HideMenu()
    {
        if (menuRoot != null) menuRoot.SetActive(false);
        else gameObject.SetActive(false); // fallback
    }

    void ShowMenu()
    {
        if (menuRoot != null) menuRoot.SetActive(true);
        else gameObject.SetActive(true);
    }

    void OnDestroy()
    {
        if (NetworkManager.Singleton == null) return;
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
    }
}
