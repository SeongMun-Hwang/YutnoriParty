using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class HammerGameManager : NetworkBehaviour
{
    [SerializeField] List<GameObject> hammerCharacterPrefabs;
    [SerializeField] List<Transform> spawnPos;
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
        if (IsServer)
        {
            Debug.Log("Spawned");
            foreach(ulong clientId in NetworkManager.ConnectedClientsIds)
            {
                if (MinigameManager.Instance.IsPlayer(clientId))
                {
                    SpawnHammerCharacterServerRpc(clientId);
                }
            }
        }
    }
    [ServerRpc(RequireOwnership =false)]
    private void SpawnHammerCharacterServerRpc(ulong clientId)
    {
        int index=PlayerManager.Instance.GetClientIndex(clientId);
        GameObject go = Instantiate(hammerCharacterPrefabs[index], spawnPos[index]);
        go.GetComponent<NetworkObject>().SpawnWithOwnership(clientId);
    }
    [ServerRpc(RequireOwnership = false)]
    public void AddForceWithHammerServerRpc(NetworkObjectReference noRef, Vector3 forceDir)
    {
        if (noRef.TryGet(out NetworkObject no))
        {
            ulong targetClientId = no.OwnerClientId; // 대상 클라이언트 ID 가져오기
            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { targetClientId } }
            };

            AddForceWithHammerClientRpc(noRef, forceDir, clientRpcParams);
        }
    }

    [ClientRpc]
    private void AddForceWithHammerClientRpc(NetworkObjectReference noRef, Vector3 forceDir, ClientRpcParams clientRpcParams = default)
    {
        Debug.Log("add force client rpc");
        if (noRef.TryGet(out NetworkObject no))
        {
            if (!no.IsOwner) return; // Owner만 실행하도록 체크

            Rigidbody rb = no.gameObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(forceDir *300f, ForceMode.Impulse);
            }
        }
    }
}
