using Unity.Netcode;
using UnityEngine;

public class LocalCameraOwner : NetworkBehaviour
{
    public Camera cam;

    public override void OnNetworkSpawn()
    {
        // only local player keeps their camera enabled
        cam.enabled = IsOwner;
        cam.gameObject.SetActive(IsOwner);
    }
}