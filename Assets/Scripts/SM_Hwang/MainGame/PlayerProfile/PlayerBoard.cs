using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class PlayerBoard : NetworkBehaviour
{
    [SerializeField] Transform playerBoardParent;
    [SerializeField] PlayerProfile playerProfilePrefab;
    [SerializeField] List<RectTransform> profileTransforms;
    public NetworkList<PlayerProfileData> playerProfileDatas = new NetworkList<PlayerProfileData>();
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
        int i = 0;
        for (; i < playerProfileDatas.Count; i++)
        {
            if (playerProfileDatas[i].clientId == player.OwnerClientId)
            {
                break;
            }
        }
        if (i >= playerProfileDatas.Count)
        {
            playerProfileDatas.Add(new PlayerProfileData
            {
                clientId = player.OwnerClientId,
                userName = ServerSingleton.Instance.clientIdToUserData[player.OwnerClientId].userName,
                score = 0
            });
        }
    }
    private void HandlePlayerDespawned(PlayerManager player)
    {
        foreach (PlayerProfileData data in playerProfileDatas)
        {
            if (data.clientId == player.OwnerClientId)
            {
                Debug.Log("Despawn profile");
                playerProfileDatas.Remove(data);
                break;
            }
        }
    }
    private Vector2[] GetCornerPositions()
    {
        float canvasWidth = Screen.width;
        float canvasHeight = Screen.height;

        return new Vector2[]
        {
        new Vector2(0, -canvasHeight)  + new Vector2(192, 108)    ,         // 좌하단
        new Vector2(canvasWidth, -canvasHeight)+ new Vector2(-192,108),    // 우하단
        new Vector2(canvasWidth, 0)   +new Vector2(-192,-108)      ,     // 우상단
        new Vector2(0, 0) + new Vector2(192,-108),                         // 좌상단
        };
    }

    private void HandleScoreboardChanged(NetworkListEvent<PlayerProfileData> changeEvent)
    {
        switch (changeEvent.Type)
        {
            case NetworkListEvent<PlayerProfileData>.EventType.Add:
                {
                    Debug.Log("profile add");
                    if (!playerProfiles.Any(x => x.clientId == changeEvent.Value.clientId))
                    {
                        int index = playerProfileDatas.IndexOf(changeEvent.Value);
                        PlayerProfile item = Instantiate(playerProfilePrefab, playerBoardParent);
                        Vector2[] dynamicCorners = GetCornerPositions();
                        RectTransform rectTransform = item.GetComponent<RectTransform>();
                        item.GetComponent<RectTransform>().anchoredPosition = dynamicCorners[index];
                        item.SetData(changeEvent.Value.clientId, changeEvent.Value.userName, changeEvent.Value.score);
                        playerProfiles.Add(item);
                    }
                }
                break;
            case NetworkListEvent<PlayerProfileData>.EventType.Remove:
                {
                    Debug.Log("profile remove");
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
                    Debug.Log("profile changed");
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