using System.Collections;
using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode.Components;
using NUnit.Framework.Internal.Filters;
using System;
using Unity.Services.Authentication;
using System.Threading.Tasks;
using TMPro;
using System.Linq;
using Unity.Collections;

public class PlayerManager : NetworkBehaviour
{
    private int numOfCharacter = 4; //전체 말 개수
    public List<GameObject> currentCharacters = new List<GameObject>(); //필드 위 말 개수
    public bool isMoving = false;
    private static PlayerManager instance;
    public static PlayerManager Instance { get { return instance; } }
    public static Action<PlayerManager> OnPlayerSpawn;
    public static Action<PlayerManager> OnPlayerDespawn;
    public static Action<ulong, int> OnGoaled;
    public override void OnNetworkSpawn()
    {
        if (IsOwner) instance = this;
        if (IsServer)
        {
            OnPlayerSpawn?.Invoke(this);
        }
    }
    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            OnPlayerDespawn?.Invoke(this);
        }
    }
    public void SpawnCharacter()
    {
        if (YutManager.Instance.Results.Count <= 0)
        {
            GameManager.Instance.announceCanvas.ShowAnnounceText("Throw First!");
            return;
        }
        if (YutManager.Instance.Results.Count == 1 && YutManager.Instance.Results[0] == YutResult.BackDo && currentCharacters.Count > 0)
        {
            GameManager.Instance.announceCanvas.ShowAnnounceText("Cannot spawn when backdo");
            return;
        }
        if (currentCharacters.Count >= numOfCharacter)
        {
            GameManager.Instance.announceCanvas.ShowAnnounceText("Character Fulled", 2f);
            return;
        }
        SpawnCharacterServerRpc(GetClientIndex());
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Keypad5))
        {
            Debug.Log(GetClientIndex());
        }
    }
    [ServerRpc(RequireOwnership = false)]
    public void SpawnCharacterServerRpc(int index, ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;
        
        GameObject go = Instantiate(GameManager.Instance.playerCharacters[index], Vector3.zero, Quaternion.identity);
        go.GetComponent<NetworkObject>().Spawn();
        go.GetComponent<NetworkObject>().ChangeOwnership(senderId);

        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { senderId } }
        };
        AddSpawnedCharacterClientRpc(go, clientRpcParams);
    }
    [ClientRpc]
    private void AddSpawnedCharacterClientRpc(NetworkObjectReference noRef, ClientRpcParams clientRpcParams = default)
    {
        currentCharacters.Add(noRef);
        noRef.TryGet(out NetworkObject no);
        if (MainGameProgress.Instance.currentCharacter != null)
        {
            MainGameProgress.Instance.currentCharacter.GetComponent<Outline>().DisableOutline();
        }
        MainGameProgress.Instance.currentCharacter = no.GetComponent<CharacterBoardMovement>();
        no.GetComponent<Outline>().EnableOutline();
    }

    [ServerRpc(RequireOwnership = default)]
    public void DespawnCharacterServerRpc(NetworkObjectReference noRef, ulong targetClientId, bool isGoal = false)
    {
        noRef.TryGet(out NetworkObject no);
        List<NetworkObject> childObjects = new List<NetworkObject>(no.GetComponentsInChildren<NetworkObject>());
        for (int i = 0; i < childObjects.Count; i++)
        {
            DespawnCharacterClientRpc(childObjects[i], isGoal);
            childObjects[i].Despawn();
            Destroy(childObjects[i]);
        }
    }
    [ClientRpc(RequireOwnership = default)]
    private void DespawnCharacterClientRpc(NetworkObjectReference noRef, bool isGoal = false)
    {
        noRef.TryGet(out NetworkObject no);
        if (no.OwnerClientId == NetworkManager.LocalClientId)
        {
            Debug.Log("Remove from list");
            PlayerManager.Instance.currentCharacters.Remove(noRef);
            PlayerManager.Instance.currentCharacters.RemoveAll(item => item == null);
            if (isGoal) numOfCharacter--;
            if (numOfCharacter == 0)
            {
                EndGame();
            }
        }
    }
    public void OverlapCharacter(GameObject parent, GameObject child)
    {
        Debug.Log("Overlap Character");
        child.GetComponent<Collider>().enabled = false;
        //parent.GetComponent<CharacterInfo>().overlappedCount++;
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
            int n = ++parent.GetComponent<CharacterInfo>().overlappedCount;
            parent.GetComponent<CharacterInfo>().overlappedCount += child.GetComponent<CharacterInfo>().overlappedCount;
            Debug.Log("parent : " + parent.GetComponent<CharacterInfo>().overlappedCount);
            Vector3 newPosition = parent.transform.position + new Vector3(0, 2, 0) * n;

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
    public void CharacterGoalIn(GameObject character)
    {
        GameManager.Instance.announceCanvas.ShowAnnounceText("Goal In!");
        isMoving = false;
        UpdateProfileServerRpc(OwnerClientId, character);
        DespawnCharacterServerRpc(character, NetworkManager.Singleton.LocalClientId, true);
    }
    [ServerRpc(RequireOwnership = default)]
    private void UpdateProfileServerRpc(ulong clientId, NetworkObjectReference noRef)
    {
        noRef.TryGet(out NetworkObject no);
        int num = no.GetComponent<CharacterInfo>().overlappedCount;
        Debug.Log("goal :" + num);
        OnGoaled?.Invoke(clientId, num + 1);
    }
    private void EndGame()
    {
        Debug.Log("Game End");
        int index = GetClientIndex();
        EndGameServerRpc(OwnerClientId, index);
    }
    [ServerRpc(RequireOwnership = default)]
    private void EndGameServerRpc(ulong id, int index)
    {
        GameManager.Instance.winnerName.Value = ServerSingleton.Instance.clientIdToUserData[id].userName;
        GameManager.Instance.winnerCharacterIndex.Value = index;
        GoToAwardSceneClientRpc("", "");
    }
    [ClientRpc]
    private void GoToAwardSceneClientRpc(FixedString128Bytes oldValue, FixedString128Bytes newValue)
    {
        foreach (var hideable in GameManager.Instance.hideableWhenOtherScene)
        {
            hideable.SetActive(false);
        }

        if (IsServer)
        {
            NetworkManager.Singleton.SceneManager.LoadScene("AwardScene", UnityEngine.SceneManagement.LoadSceneMode.Additive);
        }
    }
    public int GetClientIndex()
    {
        ulong localClientId = NetworkManager.Singleton.LocalClientId;
        var clientList = NetworkManager.Singleton.ConnectedClientsList.Select(c => c.ClientId).ToList();

        return clientList.IndexOf(localClientId);
    }
}