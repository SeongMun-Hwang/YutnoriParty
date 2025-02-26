using System.Collections;
using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

public class PlayerManager : NetworkBehaviour
{
    private int numOfCharacter = 4; //전체 말 개수
    public List<GameObject> currentCharacters=new List<GameObject>(); //필드 위 말 개수
    private static PlayerManager instance;
    public static PlayerManager Instance { get { return instance; } }
    public override void OnNetworkSpawn()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }
    public void SpawnCharacter()
    {
        if (currentCharacters.Count >= numOfCharacter)
        {
            GameManager.Instance.announceCanvas.ShowAnnounceText("Character Fulled",2f);
            return;
        }
        SpawnCharacterServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    public void SpawnCharacterServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;

        GameObject go = Instantiate(GameManager.Instance.playerCharacters[(int)senderId], Vector3.zero, Quaternion.identity);
        go.GetComponent<NetworkObject>().Spawn();
        go.GetComponent<NetworkObject>().ChangeOwnership(senderId);

        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { senderId } }
        };
        AddSpawnedCharacterClientRpc(go, clientRpcParams);
    }
    [ClientRpc]
    private void AddSpawnedCharacterClientRpc(NetworkObjectReference noRef, ClientRpcParams clientRpcParams=default)
    {
        currentCharacters.Add(noRef);
    }

    [ServerRpc(RequireOwnership = default)]
    public void DespawnCharacterServerRpc(NetworkObjectReference noRef, ulong targetClientId)
    {
        noRef.TryGet(out NetworkObject no);
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { targetClientId } }
        };
        DespawnCharacterClientRpc(noRef, clientRpcParams);

        if (no != null)
        {
            no.Despawn();
            Destroy(no);
        }
    }
    [ClientRpc]
    private void DespawnCharacterClientRpc(NetworkObjectReference noRef,ClientRpcParams clientRpcParams = default)
    {
        noRef.TryGet(out NetworkObject no);
        currentCharacters.Remove(noRef);
        currentCharacters.RemoveAll(item => item == null);
    }
    public List<GameObject> GetCurrentCharacterList()
    {
        return currentCharacters;
    }
}
