using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ClientSingleton : MonoBehaviour
{
    static ClientSingleton instance;
    public static ClientSingleton Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject singleton = new GameObject("ClientSingleton");
                instance = singleton.AddComponent<ClientSingleton>();

                DontDestroyOnLoad(singleton);
            }
            return instance;
        }
    }
    JoinAllocation allocation;
    public async Task<bool> InitAsync()
    {
        await UnityServices.InitializeAsync();
        AuthState state = await Authenticator.DoAuth();

        if (state == AuthState.Authenticated)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += OnDisconnected;
            return true;
        }
        return false;
    }

    private void OnDisconnected(ulong clientId)
    {
        //������ �ƴϰ�, ���� ����� ���� �ƴϸ�
        if (clientId != 0 && clientId != NetworkManager.Singleton.LocalClientId)
        {
            return;
        }

        bool isAwardScene = false;
        if (NetworkManager.Singleton.SceneManager != null)
        {
            foreach (var s in NetworkManager.Singleton.SceneManager.GetSynchronizedScenes())
            {
                if (s.name == "AwardScene")
                {
                    isAwardScene = true;
                    break;
                }
            }
        }

        if (isAwardScene)
        {
            return;
        }
        
        if (SceneManager.GetActiveScene().name != "MenuScene")
        {
            SceneManager.LoadScene("MenuScene");
        }
    }

    public async Task StartClientAsync(string joinCode)
    {
        try
        {
            allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
        }
        catch (RelayServiceException e)
        {
            Debug.LogException(e);
            return;
        }
        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        RelayServerData relayServerData = allocation.ToRelayServerData("dtls");
        transport.SetRelayServerData(relayServerData);

        //payload
        UserData userData = new UserData()
        {
            userName = AuthenticationService.Instance.PlayerName ?? "Annoymous",
            userAuthId = AuthenticationService.Instance.PlayerId
        };
        string payload = JsonConvert.SerializeObject(userData);
        byte[] payloadBytes = Encoding.UTF8.GetBytes(payload);
        NetworkManager.Singleton.NetworkConfig.ConnectionData = payloadBytes;

        NetworkManager.Singleton.StartClient();
    }
}