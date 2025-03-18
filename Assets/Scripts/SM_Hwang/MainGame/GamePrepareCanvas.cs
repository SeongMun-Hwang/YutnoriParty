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
    [SerializeField] TMP_InputField roomCodeField;
    [SerializeField] TextMeshProUGUI roomNameTmp;
    public override async void OnNetworkSpawn()
    {
        if (IsServer)
        {
            gameStartBtn.gameObject.SetActive(true);
        }
        var lobbyIds = await LobbyService.Instance.GetJoinedLobbiesAsync();
        Lobby lobby = await LobbyService.Instance.GetLobbyAsync(lobbyIds[0]);
        roomCodeField.text = GameManager.Instance.lobbyId.Value.ToString();
        roomNameTmp.text = GameManager.Instance.lobbyName.Value.ToString();
    }
    public void GameStart()
    {
        if (NetworkManager.ConnectedClients.Count > 1)
        {
            GameManager.Instance.mainGameProgress.StartGame();
            HostSingleton.Instance.BlockLobbyJoin();
            gameObject.GetComponent<NetworkObject>().Despawn();
        }
        else
        {
            GameManager.Instance.announceCanvas.ShowAnnounceText("플레이어가 부족합니다!", 2f);
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