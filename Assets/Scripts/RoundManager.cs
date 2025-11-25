using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class RoundManager : NetworkBehaviour
{
    public static RoundManager Instance;

    public NetworkVariable<int> roundIndex = new(0);

    void Awake() => Instance = this;

    // Called by server when P1 (host) clicks
    public void EndRound(bool win)
    {
        if (!IsServer) return;

        ShowResultClientRpc(win);
        StartCoroutine(NextRoundRoutine());
    }

    IEnumerator NextRoundRoutine()
    {
        yield return new WaitForSeconds(3f);

        roundIndex.Value++;

        int newSeed = Random.Range(int.MinValue, int.MaxValue);

        StartNewRoundClientRpc(newSeed);
    }

    [ClientRpc]
    void ShowResultClientRpc(bool win)
    {
        ResultUI.Show(win);
    }

    [ClientRpc]
    void StartNewRoundClientRpc(int seed)
    {
        ResultUI.Hide();

        if (ClueReceiver.Instance != null)
            ClueReceiver.Instance.Clear();

        // Only Host (Player1) should rebuild the town
        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsListening) return;

        if (!nm.IsHost) return;

        var placer = FindFirstObjectByType<RandomTownPlacer>();
        if (placer != null)
            placer.RebuildTown(seed);
    }
}
