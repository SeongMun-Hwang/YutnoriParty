using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Collections;
using TMPro;
using UnityEngine.SceneManagement;

public class RunGameManager : NetworkBehaviour
{
    [SerializeField] private Collider goalArea;
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private GameObject countdownCanvas;

    private NetworkList<ulong> playerIds = new NetworkList<ulong>(); // 참가한 플레이어 ID 리스트
    private List<bool> canMoveList = new List<bool>(); // 각 플레이어의 이동 상태 추적
    private List<GameObject> runObjects = new List<GameObject>(); // 각 플레이어의 Run 오브젝트 리스트

    int currentId = -1;

    [SerializeField] private GameObject winMessageUI;
    [SerializeField] private GameObject loseMessageUI;

    // 게임 상태 및 진행 관련
    [SerializeField] public NetworkVariable<bool> isPlaying;
    private bool gameStart = false;
    private bool gameEnd = false;

    [SerializeField] private List<GameObject> runPrefab =new List<GameObject>();
   

    /*추가 부분*/
    [SerializeField] public Camera runGameCamera;
    public GameObject guidePanel;
    private static RunGameManager instance;
    public static RunGameManager Instance
    {
        get { return instance; }
    }
    private void Awake()
    {
        instance = this;
    }
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            foreach (var clientPair in NetworkManager.Singleton.ConnectedClients)
            {
                ulong clientId = clientPair.Key;
                OnPlayerJoined(clientId);
            }
        }

        currentId = playerIds.IndexOf(NetworkManager.Singleton.LocalClientId);
        Debug.Log($"플레이어 ID : {currentId}");
    }

    private void OnPlayerJoined(ulong clientId)
    {
        if (!playerIds.Contains(clientId) && MinigameManager.Instance.IsPlayer(clientId))
        {
            playerIds.Add(clientId);
            canMoveList.Add(false);
            int spawnIndex = PlayerManager.Instance.GetClientIndex(clientId);
            if (spawnIndex >= runPrefab.Count) return;


            // RunPrefab 인스턴스화 및 위치 설정
            GameObject rp = Instantiate(runPrefab[spawnIndex], Vector3.zero, Quaternion.identity);
            RunGameController rc = rp.GetComponent<RunGameController>();

            // NetworkObject 설정
            NetworkObject runNetObj = rp.GetComponent<NetworkObject>();
            runNetObj.SpawnWithOwnership(clientId, true); // 클라이언트에게 소유권 부여

            RunGameController runController = rp.GetComponent<RunGameController>();
            if (runController != null)
            {
                runController.EnableControl(false); // 게임 시작 전에는 움직일 수 없도록 설정
            }
            // Run 오브젝트 리스트에 추가
            runObjects.Add(rp);

            if (playerIds.Count == MinigameManager.Instance.maxPlayers.Value)
            {
                StartCoroutine(StartGameCountdown());
            }
        }
    }

    private IEnumerator StartGameCountdown()
    {
        int countdown = 3;
        UpdateCountdownUIClientRpc(countdown, true);

        while (countdown > 0)
        {
            yield return new WaitForSeconds(1f);
            countdown--;
            UpdateCountdownUIClientRpc(countdown, true);
        }

        UpdateCountdownUIClientRpc(0, true);
        yield return new WaitForSeconds(1f);

        UpdateCountdownUIClientRpc(-1, false);

        StartGame();
    }

    [ClientRpc]
    private void UpdateCountdownUIClientRpc(int countdown, bool showCanvas)
    {
        if (countdownCanvas != null)
            countdownCanvas.SetActive(showCanvas);

        if (countdownText == null) return;

        if (countdown > 0)
            countdownText.text = countdown.ToString();
        else if (countdown == 0)
        {
            countdownText.text = "GO!";
            guidePanel.SetActive(false);
        }
        else
            countdownText.gameObject.SetActive(false);
    }

    private void StartGame()
    {
        // 모든 플레이어에게 달리기 권한 부여
        foreach (var clientId in playerIds)
        {
            int clientIndex = playerIds.IndexOf(clientId);
            if (runObjects[clientIndex] != null)
            {
                RunGameController runController = runObjects[clientIndex].GetComponent<RunGameController>();
                if (runController != null)
                {
                    runController.EnableControl(true); // 달리기 시작
                }
            }
        }

        FindAnyObjectByType<Chase>().EnableChase();
        isPlaying.Value = true; // 게임 진행 상태로 설정
    }

    private void Update()
    {
        if (isPlaying.Value)
        {
            if (!gameStart)
            {
                gameStart = true;
            }
            CheckForWinner();
            CheckRemainingPlayers();
        }
    }

    private void CheckForWinner()
    {
        if (gameEnd) return;

        foreach (var clientId in playerIds)
        {
            int clientIndex = playerIds.IndexOf(clientId);

            GameObject playerObject=null;
            if (runObjects.Count > clientIndex)
            {
                playerObject = runObjects[clientIndex];  // 각 클라이언트에 해당하는 RunGameObject를 가져옵니다.
            }

            if (playerObject != null && playerObject.TryGetComponent(out RunGameController playerController) && playerController.canMove.Value)
            {
                // 골인 지점에 도달했을 때 승리 처리
                if (goalArea.bounds.Contains(playerController.transform.position))
                {
                    EndGameServerRpc(playerController.OwnerClientId);
                    return;
                }
            }
        }
    }

    // 게임 종료 처리 - 골인 지점에 도달한 플레이어를 처리
    [ServerRpc(RequireOwnership = false)]
    private void EndGameServerRpc(ulong winnerClientId)
    {
        if (gameEnd) return;
        if (IsServer)
        {
            isPlaying.Value = false;
            gameEnd = true;
            MainGameProgress.Instance.winnerId = winnerClientId;
            GameFinishedClientRpc(winnerClientId);
            StartCoroutine(PassTheScene());
            ClearRunObjects();
        }
    }
    private void ClearRunObjects()
    {
        foreach (var obj in runObjects)
        {
            if (obj != null)
            {
                Destroy(obj); // 생성된 오브젝트 삭제
            }
        }
        runObjects.Clear(); // 리스트 초기화
    }
    public void CheckRemainingPlayers()
    {
        if (gameEnd) return;

        List<RunGameController> alivePlayers = new List<RunGameController>();

        // 살아있는 플레이어들 찾기
        foreach (var clientId in playerIds)
        {
            int clientIndex = playerIds.IndexOf(clientId);
            GameObject playerObject = null;
            if (runObjects.Count > clientIndex)
            {
                playerObject = runObjects[clientIndex];  // 각 클라이언트에 해당하는 RunGameObject를 가져옵니다.
            }

            if (playerObject != null && playerObject.TryGetComponent(out RunGameController playerController) && playerController.canMove.Value)
            {
                alivePlayers.Add(playerController);
            }
        }

        if (alivePlayers.Count == 1)
        {
            EndGameServerRpc(alivePlayers[0].OwnerClientId);
        }
    }

    // 게임 종료 후 클라이언트에게 승리 메시지 전달
    [ClientRpc]
    public void GameFinishedClientRpc(ulong winClientId)
    {
        if (MinigameManager.Instance.playerType != Define.MGPlayerType.Player) return;

        if (NetworkManager.Singleton.LocalClientId == winClientId)
        {
            winMessageUI.SetActive(true);
        }
        else
        {
            loseMessageUI.SetActive(true);
        }

        Debug.Log("게임 종료");
    }

    public IEnumerator PassTheScene()
    {
        yield return new WaitForSecondsRealtime(2f);
        //NetworkManager.Singleton.SceneManager.UnloadScene(SceneManager.GetSceneByName("RunGame"));
        MinigameManager.Instance.EndMinigame();
    }
}
