using Unity.Netcode;
using UnityEngine;

public class Obstacle : NetworkBehaviour
{
    [SerializeField] GameObject obstacle_Destructed;
    public NetworkVariable<ulong> ownerId = new NetworkVariable<ulong>(99);
    private void OnDestroy()
    {
        if (IsServer)
        {
            GameObject go = Instantiate(obstacle_Destructed,transform.position,Quaternion.identity);
            go.GetComponent<NetworkObject>().Spawn();
            Destroy(go, 2f);
        }
    }
}
