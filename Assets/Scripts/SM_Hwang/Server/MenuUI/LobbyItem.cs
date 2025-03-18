using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class LobbyItem : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI lobbyTitleText;
    [SerializeField] TextMeshProUGUI lobbyCodeText;
    [SerializeField] TextMeshProUGUI lobbyHostText;
    [SerializeField] TextMeshProUGUI lobbyPlayerText;

    LobbyList lobbyList;
    Lobby lobby;

    public void SetItem(LobbyList lobbyList, Lobby lobby)
    {
        string lobbyName = lobby.Data.ContainsKey("RoomName") ? lobby.Data["RoomName"].Value : $"{lobby.Name}";
        string hostName = lobby.Data.ContainsKey("HostName") ? lobby.Data["HostName"].Value : "Anonymous";

        lobbyTitleText.text = lobbyName;
        lobbyCodeText.text = lobby.Name;
        lobbyHostText.text = hostName;
        lobbyPlayerText.text = lobby.Players.Count + "/" + lobby.MaxPlayers;
        this.lobbyList = lobbyList;
        this.lobby = lobby;
    }
    public void JoinPressed()
    {
        lobbyList.JoinAsync(lobby);
    }
}
