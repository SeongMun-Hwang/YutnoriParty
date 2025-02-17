using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;

public class ServerSingleton : MonoBehaviour
{
    static ServerSingleton instance;
    public static ServerSingleton Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject singleton = new GameObject("ServerSingleton");
                instance = singleton.AddComponent<ServerSingleton>();

                DontDestroyOnLoad(singleton);
            }
            return instance;
        }
    }
    public Dictionary<ulong, UserData> clientIdToUserData = new Dictionary<ulong, UserData>();
    public Action<string> OnClientLeft;
    public void Init()
    {

    }
    private void OnEnable()
    {
        //ConnectionApprovalCallback : Ŀ�ؼ��� ���ö� ������ �� ����
        NetworkManager.Singleton.ConnectionApprovalCallback += ApprovalCheck;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
    }

    private void OnClientDisconnect(ulong obj)
    {
        if (clientIdToUserData.ContainsKey(obj))
        {
            string authId = clientIdToUserData[obj].userAuthId;
            clientIdToUserData.Remove(obj);
            if (obj != 0)
            {
                OnClientLeft.Invoke(authId);
            }
        }
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.ConnectionApprovalCallback -= ApprovalCheck;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
        }
    }
    //request : ���� ��û, reponse : �� ���� ����
    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        string payload = Encoding.UTF8.GetString(request.Payload);
        UserData userData = JsonConvert.DeserializeObject<UserData>(payload);
        Debug.Log("User data : " + userData.userName);

        clientIdToUserData[request.ClientNetworkId] = userData;

        response.Approved = true;
        response.Position = SpawnPoint.GetRandomSpawnPoint();
        response.Position = SpawnPoint.GetRandomSpawnPoint();
        response.CreatePlayerObject = true;
    }
}