using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class YutGrabGameManager : NetworkBehaviour
{
    //씬 언로드용
    //[SerializeField] NetworkObject gameScene;

    //UI관련
    [SerializeField] private List<TMP_Text> usernameUI;
    [SerializeField] private List<TMP_Text> scoreUI;
    [SerializeField] private GameObject winMessageUI;
    [SerializeField] private GameObject loseMessageUI;

    // 게임에 참여하는 유저 관련
    NetworkList<ulong> playerIds = new NetworkList<ulong>(); // 참가한 플레이어 ID 리스트

    //게임 진행 관련
    NetworkList<float> playerRecord = new NetworkList<float>(); // 플레이어들의 기록
    NetworkVariable<float> bestRecord = new NetworkVariable<float>();
    NetworkVariable<ulong> bestPlayerId = new NetworkVariable<ulong>();

    int noChancePlayer = 0;
    [SerializeField] List<GameObject> characterPrefabs;
    [SerializeField] List<Transform> spawnPos;
    [SerializeField] Camera watchCamera;
    private List<GameObject> playingCharacters = new List<GameObject>();

    //카메라
    [SerializeField] List<Camera> cameras = new List<Camera>();

    //public NetworkVariable<bool> isPlaying = new NetworkVariable<bool>();
    bool isGameEnd = false;
    bool isGameFinishExcute = false;

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
                playerIds.Add(clientId);
            }
            else
            {
                SetSpectorRpc(RpcTarget.Single(clientId,RpcTargetUse.Temp));
            }
        }

        //플레이에 참가한 인원수만큼
        int playerNum = playerIds.Count;
        for (int i=0; i<playerNum; i++)
        {
            playerRecord.Add(10000); //플레이어 기록 초기화
        }
        bestRecord.Value = 10000; //최고기록 초기화

        //카메라 세팅
        SetCamerasRpc(playerNum);

        InitScoreBoardUIRpc();

        StartCoroutine(StartGameTimer(5));
    }

    [Rpc(SendTo.ClientsAndHost)]
    void SetCamerasRpc(int playerNum)
    {
        //Debug.Log("참가 플레이어 : " + playerNum);

        float w = 1;
        float h = 1;
        float x = 0;
        float y = 0;

        switch (playerNum)
        {
            case 2:
                w = 0.5f;
                h = 1;

                for (int i = 0; i < playerNum; i++)
                {
                    cameras[i].rect = new Rect(i * w, y, w, h);
                    cameras[i].gameObject.SetActive(true);
                    //Debug.Log($"카메라 x:{i * w} y:{y} w:{w} h:{h}");
                }
                break;
            case 3:
                w = 0.3333f;
                h = 1;

                for (int i = 0; i < playerNum; i++)
                {
                    cameras[i].rect = new Rect(i * w, y, w, h);
                    cameras[i].gameObject.SetActive(true);
                }
                break;
            case 4:
                w = 0.5f;
                h = 0.5f;
                float[] dx = new float[4] {0, 0.5f, 0, 0.5f};
                float[] dy = new float[4] { 0, 0, 0.5f, 0.5f };

                for (int i = 0; i < playerNum; i++)
                {
                    cameras[i].rect = new Rect(x + dx[i], y + dy[i], w, h);
                    cameras[i].gameObject.SetActive(true);
                }
                break;
        }

        Debug.Log("카메라 세팅 완료");
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
        Cursor.lockState = CursorLockMode.None;
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void InitScoreBoardUIRpc()
    {
        for (int i = 0; i < playerIds.Count; i++)
        {
            usernameUI[i].transform.parent.gameObject.SetActive(true);
            foreach (PlayerProfileData data in GameManager.Instance.playerBoard.playerProfileDatas)
            {
                if (data.clientId == playerIds[i])
                {
                    usernameUI[i].text = data.userName.ToString();
                }
            }
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void UpdateScoreUIRpc()
    {
        // Debug.Log($"UI 변경 {playerScore[0]} {playerScore[1]}");
        for (int i = 0; i < MinigameManager.Instance.maxPlayers.Value; i++)
        {
            scoreUI[i].text = playerRecord[i].ToString("F2"); //소수점 두자리까지만 표시
        }
    }

    private IEnumerator StartGameTimer(int timer = 3)
    {
        GameManager.Instance.announceCanvas.ShowAnnounceTextClientRpc("아슬아슬하게 윷을 잡아라!", 2f, Color.white);
        Debug.Log("게임 스타트 5초전");
        while (timer > 0)
        {
            yield return new WaitForSecondsRealtime(1f);
            timer--;
            if (timer == 0) break;

            if(timer > 3) continue;
            GameManager.Instance.announceCanvas.ShowAnnounceTextClientRpc(timer.ToString(), 0.7f, Color.white);
            yield return null;
        }

        //isPlaying.Value = true;

        Debug.Log("게임 스타트 요청");
        for(int i = 0; i < playingCharacters.Count; i++)
        {
            playingCharacters[i].GetComponent<YutGrabController>().GameStartRpc();
        }
    }

    [Rpc(SendTo.Server)]
    public void SendReultRpc(float result, ulong playerId)
    {
        int idx = playerIds.IndexOf(playerId);

        Debug.Log("플레이어 id : " + playerId + " 인덱스 : " + idx);
        
        playerRecord[idx] = result;
        Debug.Log("플레이어 기록 : " + playerRecord[idx]);

        if (0 < result && result < bestRecord.Value)
        {
            bestRecord.Value = result;
            bestPlayerId.Value = playerId;
        }

        Debug.Log("최고 기록 : " + bestRecord.Value + " , " +  bestPlayerId.Value + "번 플레이어");

        UpdateScoreUIRpc();
    }

    [Rpc(SendTo.Server)]
    public void NoChanceRpc()
    {
        noChancePlayer++;
        Debug.Log("기회 쓴 플레이어 수 : " +  noChancePlayer + " 앞으로 : " + (playerIds.Count - noChancePlayer));
        //게임에 참가한 플레이어들 모두가 기회를 소모하면 조금 기다렸다 승자 발표
        if(noChancePlayer >= playerIds.Count && !isGameEnd)
        {
            isGameEnd = true;
            JudgeWinnerRpc();
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    void JudgeWinnerRpc()
    {
        StartCoroutine(JudgeWinner());
    }

    IEnumerator JudgeWinner()
    {
        yield return new WaitForSecondsRealtime(5f);

        if (MinigameManager.Instance.playerType != Define.MGPlayerType.Player) { yield return null; }

        if (NetworkManager.Singleton.LocalClientId == bestPlayerId.Value)
        {
            winMessageUI.SetActive(true);
        }
        else
        {
            loseMessageUI.SetActive(true);
        }

        yield return new WaitForSecondsRealtime(2f);

        FinishGameRpc();
        
        yield return null;
    }

    [Rpc(SendTo.Server)]
    void FinishGameRpc()
    {
        //서버에서 한번만 실행
        if (isGameFinishExcute) return;

        isGameFinishExcute = true;
        Debug.Log("윷 잡기 게임 종료");

        MainGameProgress.Instance.winnerId = bestPlayerId.Value;
        MinigameManager.Instance.EndMinigame();

        //남은 게임오브젝트들 삭제
        foreach (var player in playingCharacters)
        {
            player.GetComponent<NetworkObject>().Despawn();
        } 
    }
}
