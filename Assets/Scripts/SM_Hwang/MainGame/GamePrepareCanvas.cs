using Newtonsoft.Json;
using System.Text;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class GamePrepareCanvas : NetworkBehaviour
{
    [SerializeField] Button gameStartBtn;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            gameStartBtn.gameObject.SetActive(true);
        }
    }
    public void GameStart()
    {
        Debug.Log(GameManager.Instance == null);
        Debug.Log(GameManager.Instance.announceCanvas == null);
        if(NetworkManager.ConnectedClients.Count > 1)
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
