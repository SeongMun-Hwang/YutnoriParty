using Newtonsoft.Json;
using System.Linq;
using System.Text;
using TMPro;
using Unity.Netcode;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.UI;

public class GamePrepareCanvas : NetworkBehaviour
{
    [SerializeField] Button gameStartBtn;
    [SerializeField] TextMeshProUGUI roomCodeTmp;
    public override async void OnNetworkSpawn()
    {
        if (IsServer)
        {
            gameStartBtn.gameObject.SetActive(true);
        }
        var lobbyIds = await LobbyService.Instance.GetJoinedLobbiesAsync();
        Lobby lobby = await LobbyService.Instance.GetLobbyAsync(lobbyIds[0]);
        roomCodeTmp.text = lobby.Name;
    }
    public void GameStart()
    {
        if (NetworkManager.ConnectedClients.Count > 1)
        {
            GameManager.Instance.mainGameProgress.StartGame();
            gameObject.GetComponent<NetworkObject>().Despawn();
        }
        else
        {
            GameManager.Instance.announceCanvas.ShowAnnounceText("Players not enough!", 2f);
        }
    }
    public void RoomOut()
    {
        if (NetworkManager.Singleton.IsHost)
        {
            HostSingleton.Instance.ShutDown();
        }
        if (NetworkManager.Singleton.IsConnectedClient)
        {
            NetworkManager.Singleton.Shutdown();
        }
    }
}