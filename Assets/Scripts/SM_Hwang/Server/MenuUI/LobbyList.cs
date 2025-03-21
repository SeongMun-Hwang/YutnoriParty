using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class LobbyList : MonoBehaviour
{
    [SerializeField] Transform lobbyItemParent;
    [SerializeField] LobbyItem lobbyItemPrefab;
    [SerializeField] TMP_Text refreshTimeText;

    private Coroutine refreshCoroutine;
    int refreshTime;
    bool isRefreshing;
    bool isJoining;

    private void OnEnable()
    {
        RefreshList();
        refreshCoroutine = StartCoroutine(RefreshLoop());
    }

    private void OnDisable()
    {
        if (refreshCoroutine != null)
        {
            StopCoroutine(refreshCoroutine);
            refreshCoroutine = null;
        }
    }

    private IEnumerator RefreshLoop()
    {
        while (true)
        {
            refreshTime = 10;
            while (refreshTime > 0)
            {
                refreshTimeText.text = $"{refreshTime}초 후 새로고침";
                yield return new WaitForSecondsRealtime(1f);
                refreshTime--;
            }

            RefreshList();
        }
    }

    public async void RefreshList()
    {
        if (isRefreshing) return;
        isRefreshing = true;

        try
        {
            QueryLobbiesOptions options = new QueryLobbiesOptions();
            options.Count = 25; //�ҷ��� ���� ����
            options.Filters = new List<QueryFilter>
            {
                new QueryFilter(
                    QueryFilter.FieldOptions.AvailableSlots, //�����ִ� �ڸ���
                    "0", //0����
                    QueryFilter.OpOptions.GT //ū �� ������ ��
                    ),
                new QueryFilter(
                    QueryFilter.FieldOptions.IsLocked, //����
                    "0", //0==false
                    QueryFilter.OpOptions.EQ //�� ������ ��
                    )
            };
            QueryResponse lobbies=await LobbyService.Instance.QueryLobbiesAsync(options);

            if (lobbyItemParent != null)
            {
                foreach (Transform child in lobbyItemParent)
                {
                    Destroy(child.gameObject);
                }
                foreach (Lobby lobby in lobbies.Results)
                {
                    LobbyItem lobbyItem = Instantiate(lobbyItemPrefab, lobbyItemParent);
                    lobbyItem.SetItem(this, lobby);
                }
            }
            

        }
        catch (LobbyServiceException e)
        {
            Debug.LogException(e);
        }
        isRefreshing = false;
        refreshTime = 11;
    }
    public async void JoinAsync(Lobby lobby)
    {
        if (isJoining) return;
        isJoining = true;
        try
        {
            Lobby joiningLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobby.Id);
            string joinCode = joiningLobby.Data["JoinCode"].Value;
            await ClientSingleton.Instance.StartClientAsync(joinCode);

        }
        catch (LobbyServiceException e)
        {
            Debug.LogException(e);
        }
        isJoining=false;
    }
}
