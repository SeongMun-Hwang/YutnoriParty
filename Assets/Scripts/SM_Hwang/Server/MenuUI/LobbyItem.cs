using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class LobbyItem : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI lobbyTitleText;
    [SerializeField] TextMeshProUGUI lobbyPlayerText;

    LobbyList lobbyList;
    Lobby lobby;

    public void SetItem(LobbyList lobbyList, Lobby lobby)
    {
        lobbyTitleText.text = lobby.Name;
        lobbyPlayerText.text = lobby.Players.Count + "/" + lobby.MaxPlayers;
        this.lobbyList = lobbyList;
        this.lobby = lobby;
    }
    public void JoinPressed()
    {
        lobbyList.JoinAsync(lobby);
    }
}
