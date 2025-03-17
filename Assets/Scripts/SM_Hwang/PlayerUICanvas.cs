using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerUICanvas : NetworkBehaviour
{
    [SerializeField] private List<GameObject> playerUIs;
    private NetworkList<ulong> playerIds = new NetworkList<ulong>();

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            foreach (ulong clientId in NetworkManager.ConnectedClientsIds)
            {
                if(MinigameManager.Instance.IsPlayer(clientId))
                {
                    playerIds.Add(clientId);
                }
            }
        }
        if (IsClient)
        {
            foreach (ulong clientId in NetworkManager.ConnectedClientsIds)
            {
                int index = PlayerManager.Instance.GetClientIndex(clientId);
                PlayerProfileData data = GameManager.Instance.playerBoard.playerProfileDatas[index];
                playerUIs[index].SetActive(true);
                playerUIs[index].GetComponent<MiniGameProfile>().SetName(data.userName.ToString());
                if (playerIds.Contains(clientId))
                {                 
                    playerUIs[index].GetComponent<MiniGameProfile>().SetStatus("Live");
                }
                else
                {
                    playerUIs[index].GetComponent<MiniGameProfile>().SetStatus("Spectator");
                }
            }
            }
    }

    [ClientRpc]
    public void SetPlayerDeadClientRpc(ulong clientId)
    {
        int index = PlayerManager.Instance.GetClientIndex(clientId);
        playerUIs[index].gameObject.GetComponent<MiniGameProfile>().SetStatus("Dead");
    }
}
