using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    private static GameManager instance;
    public static GameManager Instance { get { return instance; } }

    //유니티에서 물려놓은 변수가 있으면 OnNetworkSpawn이 아닌 Awake에서 초기화해야 됨
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }
    public AnnounceCanvas announceCanvas;
    public GameObject inGameCanvas;
    public MainGameProgress mainGameProgress;
    public PlayerBoard playerBoard;
    public Node startNode;
    public Node blackNode;
    public List<GameObject> playerCharacters;
    public NetworkVariable<FixedString128Bytes> lobbyId = new NetworkVariable<FixedString128Bytes>();
    public override void OnNetworkSpawn()
    {
        if(IsServer) lobbyId.Value = HostSingleton.Instance.ReturnJoinCode();
    }
}
