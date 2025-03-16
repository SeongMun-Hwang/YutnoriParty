using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class HammerGameManager : NetworkBehaviour
{
    [SerializeField] List<GameObject> hammerCharacterPrefabs;
    private static HammerGameManager instance;
    public static HammerGameManager Instance
    {
        get {return instance;}
    }
    private void Awake()
    {
        instance = this;
    }
    public override void OnNetworkSpawn()
    {
        if (IsClient)
        {
            if (MinigameManager.Instance.IsPlayer(NetworkManager.LocalClientId))
            {
                SpawnHammerCharacterServerRpc(NetworkManager.LocalClientId);
            }
        }
    }
    [ServerRpc(RequireOwnership =false)]
    private void SpawnHammerCharacterServerRpc(ulong clientId)
    {
        int index=PlayerManager.Instance.GetClientIndex(clientId);
        GameObject go = Instantiate(hammerCharacterPrefabs[index]);
    }
}
