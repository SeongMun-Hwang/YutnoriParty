using Unity.Netcode;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;

public class CharacterInfo : NetworkBehaviour
{
    [ServerRpc(RequireOwnership = false)]
    public void DespawnServerRpc()
    {
        if (NetworkManager.IsServer)
        {
            gameObject.GetComponent<NetworkObject>().Despawn();
            Destroy(gameObject);
        }
    }
}
