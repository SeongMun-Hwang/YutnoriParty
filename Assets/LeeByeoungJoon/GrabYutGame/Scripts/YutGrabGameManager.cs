using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class YutGrabGameManager : NetworkBehaviour
{
    //씬 언로드용
    //[SerializeField] NetworkObject gameScene;

    //UI관련
    [SerializeField] private List<TMP_Text> usernameUI;
    [SerializeField] private List<TMP_Text> scoreUI;
    [SerializeField] private GameObject winMessageUI;
    [SerializeField] private GameObject drawMessageUI;
    [SerializeField] private GameObject loseMessageUI;
    [SerializeField] private GameObject howToPlayUI;
    [SerializeField] private GameObject guidePanel;
    [SerializeField] private Image outLine;

    //카메라 UI
    [SerializeField] Canvas camOutlineCanvas;

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

        int index = 0;
        foreach (ulong clientId in NetworkManager.ConnectedClientsIds)
        {
            if (MinigameManager.Instance.IsPlayer(clientId))
            {
                SpawnCharacterRpc(clientId, index++);
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
        //각종 초기화
        InitScoreBoardUIRpc();
        InItEventsRpc();

        StartCoroutine(StartGameTimer(5));
    }

    public override void OnNetworkDespawn()
    {
        playerRecord.OnListChanged -= UpdateScoreUI;
    }

    [Rpc(SendTo.ClientsAndHost)]
    void InItEventsRpc()
    {
        playerRecord.OnListChanged += UpdateScoreUI;
    }

    private void Update()
    {
        //if (Input.GetKeyDown(KeyCode.G))
        //{
        //    Debug.Log("플레이어 기록 출력");
        //    for(int i=0; i<playerIds.Count; i++)
        //    {
        //        Debug.Log("플레이어 인덱스 : " + i + " 기록 : " + playerRecord[i]);
        //    }
        //}
    }

    [Rpc(SendTo.ClientsAndHost)]
    void SetCamerasRpc(int playerNum)
    {
        //Debug.Log("참가 플레이어 : " + playerNum);

        float w = 1;
        float h = 1;
        float x = 0;
        float y = 0;
        Rect rect = new Rect();

        switch (playerNum)
        {
            case 2:
                w = 0.5f;
                h = 1;

                for (int i = 0; i < playerNum; i++)
                {
                    rect = new Rect(i * w, y, w, h);
                    cameras[i].rect = rect;
                    cameras[i].gameObject.SetActive(true);

                    //Debug.Log($"카메라 x:{i * w} y:{y} w:{w} h:{h}");
                }
                break;
            case 3:
                w = 0.3333f;
                h = 1;

                for (int i = 0; i < playerNum; i++)
                {
                    rect = new Rect(i * w, y, w, h);
                    cameras[i].rect = rect;
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
                    rect = new Rect(x + dx[i], y + dy[i], w, h);
                    cameras[i].rect = rect;
                    cameras[i].gameObject.SetActive(true);
                }
                break;
        }

        ulong id = NetworkManager.Singleton.LocalClientId;
        int idx = -1;
        if (playerIds.Contains(id))
        {
            idx = playerIds.IndexOf(id);
            camOutlineCanvas.worldCamera = cameras[idx];
            Color color = GameManager.Instance.playerColors[PlayerManager.Instance.GetClientIndex(id)];
            outLine.color = color;
            outLine.GetComponent<Shadow>().effectColor = color;

            StartCoroutine(BlinkingOutline());
        }

        Debug.Log("카메라 세팅 완료");
    }

    IEnumerator BlinkingOutline()
    {
        int blinkNum = 3;
        WaitForSecondsRealtime time = new WaitForSecondsRealtime(0.5f);
        while (blinkNum > 0)
        {
            yield return time;
            outLine.gameObject.SetActive(false);

            yield return time;
            outLine.gameObject.SetActive(true);

            blinkNum--;
        }
    }

    [Rpc(SendTo.Server)]
    void SpawnCharacterRpc(ulong id, int i)
    {
        int index = PlayerManager.Instance.GetClientIndex(id);
        GameObject go = Instantiate(characterPrefabs[index], spawnPos[i]);
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
            //usernameUI[i].transform.parent.gameObject.SetActive(true);
            foreach (PlayerProfileData data in GameManager.Instance.playerBoard.playerProfileDatas)
            {
                if (data.clientId == playerIds[i])
                {
                    usernameUI[i].text = data.userName.ToString();
                }
            }
        }
    }

    //[Rpc(SendTo.ClientsAndHost)]
    //private void UpdateScoreUIRpc()
    //{
    //    // Debug.Log($"UI 변경 {playerScore[0]} {playerScore[1]}");
    //    for (int i = 0; i < playerIds.Count; i++)
    //    {
    //        int id = (int)playerIds[i];
    //        //기회 쓴 애들만 기록 보이게 함
    //        if (!usernameUI[id].transform.parent.gameObject.activeSelf)
    //        {
    //            usernameUI[id].transform.parent.gameObject.SetActive(true);
    //        }

    //        Debug.Log("i : " + id + " 기록 : " + playerRecord[id]);

    //        //초기값아니고 기록이 있을때만 업데이트
    //        if (playerRecord[id] < 10000.0f)
    //        {
    //            //센티미터 단위로 표기
    //            int centimeteres = (int)(playerRecord[id] * 100);
                
    //            scoreUI[id].text = $"{centimeteres}cm";
    //        }
    //        else//기록 없으면 X표기
    //        {
    //            scoreUI[id].text = "X";
    //        }
    //    }
    //}

    private void UpdateScoreUI(NetworkListEvent<float> changeEvent)
    {
        // Debug.Log($"UI 변경 {playerScore[0]} {playerScore[1]}");
        for (int i = 0; i < playerIds.Count; i++)
        {
            int id = (int)playerIds[i];
            //기회 쓴 애들만 기록 보이게 함
            if (!usernameUI[id].transform.parent.gameObject.activeSelf)
            {
                usernameUI[id].transform.parent.gameObject.SetActive(true);
            }

            Debug.Log("i : " + i + " 기록 : " + playerRecord[i]);

            //초기값아니고 기록이 있을때만 업데이트
            if (playerRecord[i] < 10000.0f)
            {
                //센티미터 단위로 표기
                int centimeteres = (int)((playerRecord[i]) * 100);

                scoreUI[id].text = $"{centimeteres}cm";
            }
            else//기록 없으면 X표기
            {
                scoreUI[id].text = "X";
            }
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

        DeactiveHowToPlayUIRpc();

        //isPlaying.Value = true;

        Debug.Log("게임 스타트 요청");
        for(int i = 0; i < playingCharacters.Count; i++)
        {
            playingCharacters[i].GetComponent<YutGrabController>().GameStartRpc();
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    void DeactiveHowToPlayUIRpc()
    {
        howToPlayUI.SetActive(false);
        guidePanel.SetActive(false);
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

        //StartCoroutine(DelayUpdateScore(0.5f));
    }

    //IEnumerator DelayUpdateScore(float time)
    //{
    //    Debug.Log("스코어 업데이트 기다림.. "  + time);
    //    yield return new WaitForSeconds(time);

    //    Debug.Log("스코어 업데이트 요청");
    //    UpdateScoreUIRpc();
    //}

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

        ulong winnerId = bestPlayerId.Value;

        //최고기록이 초기화 값에서 안벗어났으면 무승부
        if (bestRecord.Value > 9999)
        {
            winnerId = 99;
            drawMessageUI.SetActive(true);
        }
        else //기록 있으면 정상 진행
        {
            if (NetworkManager.Singleton.LocalClientId == winnerId)
            {
                winMessageUI.SetActive(true);
            }
            else
            {
                loseMessageUI.SetActive(true);
            }
        }

        yield return new WaitForSecondsRealtime(2f);

        FinishGameRpc(winnerId);
        
        yield return null;
    }

    [Rpc(SendTo.Server)]
    void FinishGameRpc(ulong winnerId)
    {
        //서버에서 한번만 실행
        if (isGameFinishExcute) return;

        isGameFinishExcute = true;
        Debug.Log("윷 잡기 게임 종료");

        MainGameProgress.Instance.winnerId = winnerId;
        MinigameManager.Instance.EndMinigame();

        //남은 게임오브젝트들 삭제
        foreach (var player in playingCharacters)
        {
            player.GetComponent<NetworkObject>().Despawn();
        } 
    }
}
