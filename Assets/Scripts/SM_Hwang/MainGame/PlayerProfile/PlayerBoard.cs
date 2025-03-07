using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class PlayerBoard : NetworkBehaviour
{
    [SerializeField] Transform playerBoardParent;
    [SerializeField] PlayerProfile playerProfilePrefab;
    [SerializeField] List<RectTransform> profileTransforms;
    Vector2[] corners = new Vector2[4]
{
                            new Vector2(208, -972),  // 좌하단
                            new Vector2( 1708 , -969),  // 우하단
                            new Vector2( 1708 ,  -114),  // 우상단
                            new Vector2(208 ,  -114)   // 좌상단
};
    NetworkList<PlayerProfileData> playerProfileDatas = new NetworkList<PlayerProfileData>();
    List<PlayerProfile> playerProfiles = new List<PlayerProfile>();
    public override void OnNetworkSpawn()
    {
        if (IsClient)
        {
            playerProfileDatas.OnListChanged += HandleScoreboardChanged;
            foreach (PlayerProfileData data in playerProfileDatas)
            {
                HandleScoreboardChanged(new NetworkListEvent<PlayerProfileData>
                {
                    Type = NetworkListEvent<PlayerProfileData>.EventType.Add,
                    Value = data
                });
            }
        }
        if (IsServer)
        {
            PlayerManager[] players = FindObjectsByType<PlayerManager>(FindObjectsSortMode.None);
            foreach (PlayerManager player in players)
            {
                HandlePlayerSpawned(player);
            }

            PlayerManager.OnPlayerSpawn += HandlePlayerSpawned;
            PlayerManager.OnPlayerDespawn += HandlePlayerDespawned;
            PlayerManager.OnGoaled += HandleGetScore;
        }
    }
    public override void OnNetworkDespawn()
    {
        if (IsClient)
        {
            playerProfileDatas.OnListChanged -= HandleScoreboardChanged;
        }
        if (IsServer)
        {
            PlayerManager.OnPlayerSpawn -= HandlePlayerSpawned;
            PlayerManager.OnPlayerDespawn -= HandlePlayerDespawned;
            PlayerManager.OnGoaled -= HandleGetScore;
        }
    }
    private void HandlePlayerSpawned(PlayerManager player)
    {
        playerProfileDatas.Add(new PlayerProfileData
        {
            clientId = player.OwnerClientId,
            userName = ServerSingleton.Instance.clientIdToUserData[player.OwnerClientId].userName,
            score = 0
        });
    }
    private void HandlePlayerDespawned(PlayerManager player)
    {
        foreach (PlayerProfileData data in playerProfileDatas)
        {
            if (data.clientId == player.OwnerClientId)
            {
                playerProfileDatas.Remove(data);
                break;
            }
        }
    }
    private void HandleScoreboardChanged(NetworkListEvent<PlayerProfileData> changeEvent)
    {
        switch (changeEvent.Type)
        {
            case NetworkListEvent<PlayerProfileData>.EventType.Add:
                {
                    if (!playerProfiles.Any(x => x.clientId == changeEvent.Value.clientId))
                    {
                        int index = playerProfileDatas.IndexOf(changeEvent.Value);
                        PlayerProfile item = Instantiate(playerProfilePrefab, playerBoardParent);
                        item.GetComponent<RectTransform>().anchoredPosition = corners[index];
                        item.SetData(changeEvent.Value.clientId, changeEvent.Value.userName, changeEvent.Value.score);
                        playerProfiles.Add(item);
                    }
                }
                break;
            case NetworkListEvent<PlayerProfileData>.EventType.Remove:
                {
                    PlayerProfile item = playerProfiles.FirstOrDefault(x => x.clientId == changeEvent.Value.clientId);
                    if (item != null)
                    {
                        playerProfiles.Remove(item);
                        Destroy(item.gameObject);
                    }
                }
                break;
            case NetworkListEvent<PlayerProfileData>.EventType.Value:
                {
                    PlayerProfile item = playerProfiles.FirstOrDefault(x => x.clientId == changeEvent.Value.clientId);
                    if (item != null)
                    {
                        item.SetData(changeEvent.Value.clientId, changeEvent.Value.userName, changeEvent.Value.score);
                    }
                }
                break;
        }
    }
    private void HandleGetScore(ulong clientId, int score)
    {
        for (int i = 0; i < playerProfileDatas.Count; i++)
        {
            if (playerProfileDatas[i].clientId == clientId)
            {
                playerProfileDatas[i] = new PlayerProfileData
                {
                    clientId = clientId,
                    userName = playerProfileDatas[i].userName,
                    score = playerProfileDatas[i].score + score
                };
                break;
            }
        }
    }
}
