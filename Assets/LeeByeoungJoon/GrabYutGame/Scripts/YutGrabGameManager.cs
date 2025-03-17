using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class YutGrabGameManager : NetworkBehaviour
{
    // 게임에 참여하는 유저 관련
    private NetworkList<ulong> playerIds = new NetworkList<ulong>(); // 참가한 플레이어 ID 리스트
    int currentId = -1;

    [SerializeField] List<GameObject> characterPrefabs;
    [SerializeField] List<Transform> spawnPos;
    [SerializeField] Camera watchCamera;
    private List<GameObject> playingCharacters = new List<GameObject>();

    public NetworkVariable<bool> isPlaying = new NetworkVariable<bool>();

    private static YutGrabGameManager instance;
    public static YutGrabGameManager Instance
    {
        get { return instance; }
    }
    private void Awake()
    {
        instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if(!IsServer) return;

        foreach (ulong clientId in NetworkManager.ConnectedClientsIds)
        {
            if (MinigameManager.Instance.IsPlayer(clientId))
            {
                SpawnCharacterRpc(clientId);
            }
            else
            {
                SetSpectorRpc(RpcTarget.Single(clientId,RpcTargetUse.Temp));
            }
        }

        StartCoroutine(StartGameTimer(5));
    }

    [Rpc(SendTo.Server)]
    void SpawnCharacterRpc(ulong id)
    {
        int index = PlayerManager.Instance.GetClientIndex(id);
        GameObject go = Instantiate(characterPrefabs[index], spawnPos[index]);
        go.GetComponent<NetworkObject>().SpawnWithOwnership(id);
        playingCharacters.Add(go);
    }

    [Rpc(SendTo.SpecifiedInParams)]
    void SetSpectorRpc(RpcParams rpcParams)
    {
        watchCamera.gameObject.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        watchCamera = Camera.main;
    }

    private IEnumerator StartGameTimer(int timer = 3)
    {
        GameManager.Instance.announceCanvas.ShowAnnounceTextClientRpc("아슬아슬하게 윷을 잡아라!", 2f, Color.white);

        while (timer < 4)
        {
            yield return new WaitForSecondsRealtime(1f);
            timer--;
            if (timer == 0) break;
            GameManager.Instance.announceCanvas.ShowAnnounceTextClientRpc(timer.ToString(), 0.7f, Color.white);
            yield return null;
        }

        isPlaying.Value = true;
    }
}
