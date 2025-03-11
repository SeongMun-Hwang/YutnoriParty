using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Lobbies;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using System.Collections.Generic;
using Unity.Services.Lobbies.Models;
using System.Collections;
using Newtonsoft.Json;
using System.Text;
using Unity.Services.Authentication;
using System;
using WebSocketSharp;
public class HostSingleton : MonoBehaviour
{
    static HostSingleton instance;
    public static HostSingleton Instance
    {
        get
        {
            if(instance == null)
            {
                GameObject singleton = new GameObject("HostSingleton");
                instance = singleton.AddComponent<HostSingleton>();

                DontDestroyOnLoad(singleton);
            }
            return instance;
        }
    }
    const int MaxConnections = 4;
    Allocation allocation;
    string joinCode;
    string lobbyId;

    public async Task StartHostAsync()
    {
        try
        {
            allocation = await RelayService.Instance.CreateAllocationAsync(MaxConnections);
            joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            Debug.Log(joinCode);
        }
        catch(RelayServiceException e)
        {
            Debug.LogException(e);
            return;
        }
        UnityTransport transport=NetworkManager.Singleton.GetComponent<UnityTransport>();
        //ToRelayServerData �ɼ� : udp - ,dtls - ,ws - ,wss - 
        RelayServerData relayServerData = allocation.ToRelayServerData("dtls");
        transport.SetRelayServerData(relayServerData);

        //�κ� ����
        try
        {
            CreateLobbyOptions options=new CreateLobbyOptions();
            options.IsPrivate = false;
            options.Data = new Dictionary<string, Unity.Services.Lobbies.Models.DataObject>
            {
                {
                    //Unity.Services.Lobbies.Models.DataObject.VisibilityOptions.Member : �������� �� �� �ִ°�
                    "JoinCode",new Unity.Services.Lobbies.Models.DataObject(Unity.Services.Lobbies.Models.DataObject.VisibilityOptions.Member, joinCode)
                }
            };
            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(joinCode, MaxConnections, options);
            lobbyId = lobby.Id;
            StartCoroutine(HeartBeatLobby(15));
        }
        catch(LobbyServiceException e)
        {
            Debug.LogException(e);
            return;
        }
        //������� �κ�
        ServerSingleton.Instance.Init();

        UserData userData = new UserData()
        {
            userName = AuthenticationService.Instance.PlayerName ?? "Annoymous",
            userAuthId = AuthenticationService.Instance.PlayerId
        };
        string payload = JsonConvert.SerializeObject(userData);
        byte[] payloadBytes = Encoding.UTF8.GetBytes(payload);
        NetworkManager.Singleton.NetworkConfig.ConnectionData = payloadBytes;

        ServerSingleton.Instance.OnClientLeft += HandleClientLeft;

        NetworkManager.Singleton.StartHost();
        NetworkManager.Singleton.SceneManager.LoadScene("MainGame", UnityEngine.SceneManagement.LoadSceneMode.Single);
    }

    private async void HandleClientLeft(string authId)
    {
        try
        {
            await LobbyService.Instance.RemovePlayerAsync(lobbyId, authId);
        }
        catch(LobbyServiceException e){ 
            Debug.LogException(e);
        }
    }

    IEnumerator HeartBeatLobby(float waitTimeSeconds)
    {
        var delay = new WaitForSecondsRealtime(waitTimeSeconds);
        while (true)
        {
            LobbyService.Instance.SendHeartbeatPingAsync(lobbyId);
            yield return delay;
        }
    }
    public async void ShutDown()
    {
        StopAllCoroutines();
        if (!lobbyId.IsNullOrEmpty())
        {
            try
            {
                await LobbyService.Instance.DeleteLobbyAsync(lobbyId);
            }
            catch(LobbyServiceException e)
            {
                Debug.LogException(e);
            }
            lobbyId = null;
        }
        ServerSingleton.Instance.OnClientLeft-= HandleClientLeft;
    }
    public async void BlockLobbyJoin()
    {
        if (lobbyId.IsNullOrEmpty()) return;
        try
        {
            await LobbyService.Instance.UpdateLobbyAsync(lobbyId, new UpdateLobbyOptions
            {
                IsPrivate = true
            });
            Debug.Log("Block lobby join");
        }
        catch (LobbyServiceException e)
        {
            Debug.LogException(e);
        }
    }
}
