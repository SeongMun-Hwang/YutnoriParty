using System.Collections;
using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

public class PlayerManager : NetworkBehaviour
{
    public GameObject playerCharacter; //유저 별 캐릭터
    private int numOfCharacter = 4; //전체 말 개수
    public List<GameObject> currentCharacters=new List<GameObject>(); //필드 위 말 개수
    private static PlayerManager instance;
    public static PlayerManager Instance { get { return instance; } }
    public override void OnNetworkSpawn()
    {
        StartCoroutine(WaitUntilGameManagerLoad());
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
    private IEnumerator WaitUntilGameManagerLoad()
    {
        while (GameManager.Instance == null)
        {
            yield return null;
        }
        Initialize();
    }
    private void Initialize()
    {
        GetInfo();
    }
    private void GetInfo()
    {
        playerCharacter = GameManager.Instance.playerCharacters[(int)NetworkManager.LocalClientId];
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
    public void DespawnCharacterServerRpc(NetworkObjectReference noRef)
    {
        currentCharacters.Remove(noRef);
        noRef.TryGet(out NetworkObject no);
        if(no != null)
        {
            no.Despawn();
            Destroy(no);
        }
    }
    public List<GameObject> GetCurrentCharacterList()
    {
        return currentCharacters;
    }
}
