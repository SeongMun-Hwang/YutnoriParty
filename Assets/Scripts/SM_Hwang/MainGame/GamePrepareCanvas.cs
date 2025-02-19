using Newtonsoft.Json;
using System.Text;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class GamePrepareCanvas :NetworkBehaviour
{
    [SerializeField] TextMeshProUGUI playerTmp;
    [SerializeField] Button gameStartBtn;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            gameStartBtn.gameObject.SetActive(true);
        }
    }
    private void Update()
    {
        string info="";
        for(int i = 0; i < NetworkManager.ConnectedClients.Count; i++)
        {
           info += NetworkManager.ConnectedClientsIds[i].ToString() + ":" + NetworkManager.ConnectedClientsIds[i]+"\n";
        }
        playerTmp.text = info;
    }
    public void GameStart()
    {
        if(NetworkManager.ConnectedClients.Count > 1)
        {
            GameManager.Instance.mainGameProgress.StartGame();
            gameObject.GetComponent<NetworkObject>().Despawn();
        }
        else
        {
            GameManager.Instance.announceCanvas.ShowAnnounceTextClientRpc("Players not enough!", 2f);
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
