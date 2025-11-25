using UnityEngine;
using Unity.Netcode;

public class CameraSync : NetworkBehaviour
{
    public NetworkVariable<Vector3> camPos = new(
        Vector3.zero,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;
        camPos.Value = transform.position;
    }

    void Update()
    {
        if (!IsOwner) return;

        // example local camera movement:
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 delta = new Vector3(h, v, 0) * 8f * Time.deltaTime;

        transform.position += delta;
        camPos.Value = transform.position;
    }

    void LateUpdate()
    {
        if (IsOwner) return;
        transform.position = camPos.Value;
    }
}
