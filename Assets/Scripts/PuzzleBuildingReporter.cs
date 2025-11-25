using Unity.Netcode;
using UnityEngine;

public class PuzzleBuildingReporter : NetworkBehaviour
{
    public void ReportClueNow()
    {
        if (!IsOwner) return;

        var buildings = FindObjectsByType<BuildingMarker>(FindObjectsSortMode.None);

        BuildingMarker clue = null;
        foreach (var b in buildings)
        {
            if (b != null && b.isClue)
            {
                clue = b;
                break;
            }
        }

        if (clue == null)
        {
            Debug.LogWarning("ReportClueNow: no isClue building found.");
            return;
        }

        SendClueBuildingServerRpc((Vector2)clue.transform.position);
    }

    [ServerRpc]
    void SendClueBuildingServerRpc(Vector2 cluePos)
    {
        ClueManager.Instance.ReceiveClueFromP1(cluePos);
    }
}
