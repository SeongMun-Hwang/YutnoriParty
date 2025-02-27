using System.Collections;
using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode.Components;

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
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { targetClientId } }
        };
        noRef.TryGet(out NetworkObject no);
        List<NetworkObject> childObjects = new List<NetworkObject>(no.GetComponentsInChildren<NetworkObject>());
        for(int i=0;i<childObjects.Count;i++)
        {
            DespawnCharacterClientRpc(childObjects[i], clientRpcParams);
            childObjects[i].Despawn();
        }
    }
    [ClientRpc]
    private void DespawnCharacterClientRpc(NetworkObjectReference noRef,ClientRpcParams clientRpcParams = default)
    {
        noRef.TryGet(out NetworkObject no);
        currentCharacters.Remove(noRef);
        currentCharacters.RemoveAll(item => item == null);
    }
    public void OverlapCharacter(GameObject parent, GameObject child)
    {
        Debug.Log("Overlap Character");
        child.GetComponent<Collider>().enabled = false;
        OverlapCharacterServerRpc(parent, child);
    }
    /*말 업을 때 부모 지정 ServerRpc*/
    //Reparent는 서버에서 해야됨
    [ServerRpc(RequireOwnership = default)]
    private void OverlapCharacterServerRpc(NetworkObjectReference parentNo, NetworkObjectReference childNo)
    {
        if (parentNo.TryGet(out NetworkObject parent) && childNo.TryGet(out NetworkObject child))
        {
            child.TrySetParent(parent.transform);
            Vector3 newPosition = parent.transform.position + new Vector3(0, 2, 0);

            // 서버에서 위치 변경
            child.transform.position = newPosition;

            // 클라이언트에 위치 동기화 요청
            UpdateChildPositionClientRpc(childNo, newPosition);
        }
    }
    [ClientRpc]
    private void UpdateChildPositionClientRpc(NetworkObjectReference childNo, Vector3 newPosition)
    {
        if (childNo.TryGet(out NetworkObject child))
        {
            child.transform.position = newPosition;
        }
    }
}
