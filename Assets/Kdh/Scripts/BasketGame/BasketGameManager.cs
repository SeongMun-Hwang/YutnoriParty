using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.SceneManagement;

public class BasketGameManager : NetworkBehaviour
{
    [SerializeField] private float gameDuration = 60f;
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private TextMeshProUGUI remainingTimeText;
    [SerializeField] private TextMeshProUGUI scoreBoardText; // 모든 플레이어 점수 표시
    [SerializeField] private GameObject countdownCanvas;
    [SerializeField] private GameObject winnerTextCanvas;
    [SerializeField] private GameObject winMessageUI;
    [SerializeField] private GameObject loseMessageUI;

    private NetworkList<ulong> playerIds = new NetworkList<ulong>(); 
    private List<GameObject> basketObjects = new List<GameObject>();
    int currentId = -1;
    private Dictionary<ulong, NetworkVariable<int>> playerScores = new Dictionary<ulong, NetworkVariable<int>>(); // 개별 플레이어 점수 관리
    [SerializeField] public NetworkVariable<bool> isPlaying;
    private bool gameStart = false;
    private bool gameEnd = false;

    private float remainingTime;
    [SerializeField] public Camera basketGameCamera;
    [SerializeField] private List<GameObject> basketPrefab = new List<GameObject>();
    [SerializeField] public List<Transform> spawnPos = new List<Transform>();
    private static BasketGameManager instance;
    public static BasketGameManager Instance
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
            remainingTime = gameDuration;
            InitializePlayerScores();
            
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
            
            int spawnIndex = playerIds.IndexOf(clientId);
            if (spawnIndex >= spawnPos.Count) return;
            Vector3 spawnPosition = spawnPos[spawnIndex].position;

            // RunPrefab 인스턴스화 및 위치 설정
            GameObject bp = Instantiate(basketPrefab[spawnIndex], spawnPosition, Quaternion.identity);
            BasketGameController bc = bp.GetComponent<BasketGameController>();

            // NetworkObject 설정
            NetworkObject runNetObj = bp.GetComponent<NetworkObject>();
            runNetObj.SpawnWithOwnership(clientId, true); // 클라이언트에게 소유권 부여

            BasketGameController basketController = bp.GetComponent<BasketGameController>();
            if (basketController != null)
            {
                basketController.EnableControl(false); // 게임 시작 전에는 움직일 수 없도록 설정
            }
            // Run 오브젝트 리스트에 추가
            basketObjects.Add(bp);

            if (playerIds.Count == MinigameManager.Instance.maxPlayers.Value)
            {
                StartCoroutine(StartCountdown());
            }
        }
    }
    private void InitializePlayerScores()
    {
        foreach (var client in NetworkManager.Singleton.ConnectedClients)
        {
            ulong clientId = client.Key;
            playerScores[clientId] = new NetworkVariable<int>(0);
            UpdateScoreUI();
            playerScores[clientId].OnValueChanged += (oldValue, newValue) => UpdateScoreUI();
        }
    }
  
    private IEnumerator StartCountdown()
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

        StartGameClientRpc();
        StartCoroutine(GameTimer());
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
            countdownText.text = "GO!";
        else
            countdownText.gameObject.SetActive(false);
    }

    [ClientRpc]
    private void StartGameClientRpc()
    {

        if (scoreBoardText != null)
        {
            scoreBoardText.transform.parent.gameObject.SetActive(true);
        }

        foreach (var clientId in playerIds)
        {
            int clientIndex = playerIds.IndexOf(clientId);
            if (basketObjects[clientIndex] != null)
            {
                BasketGameController runController = basketObjects[clientIndex].GetComponent<BasketGameController>();
                if (runController != null)
                {
                    runController.EnableControl(true);
                }
            }
        }
        isPlaying.Value = true;
    }
    private void Update()
    {
        if (isPlaying.Value)
        {
            if (!gameStart)
            {
                gameStart = true;
            }
        }
    }

            private IEnumerator GameTimer()
    {
        while (remainingTime > 0 && !gameEnd)
        {
            yield return new WaitForSeconds(1f);
            remainingTime--;
            UpdateRemainingTimeClientRpc((int)remainingTime);
        }

        if (!gameEnd)
        {
            DetermineWinner();
        }
    }

    [ClientRpc]
    private void UpdateRemainingTimeClientRpc(int time)
    {
        if (remainingTimeText != null)
        {
            if (time > 0)
                remainingTimeText.text = $"Time: {time}s";
            else
                remainingTimeText.text = "Finish!";
        }
    }

    // 과일에 의해 점수 추가 요청
    public void AddScore(ulong playerId, int points)
    {
        if (!IsServer)  // 클라이언트에서 호출 시, 서버로 요청
        {
            AddScoreServerRpc(playerId, points);
        }
        else  // 서버에서 직접 갱신
        {
            if (playerScores.ContainsKey(playerId))
            {
                playerScores[playerId].Value += points;
                UpdateScoreUI();  // 서버에서 점수 갱신 후 UI 업데이트
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void AddScoreServerRpc(ulong playerId, int points)
    {
        if (gameEnd) return;

        if (playerScores.ContainsKey(playerId))
        {
            playerScores[playerId].Value += points;  // 서버에서 점수 갱신
            UpdateScoreUI();  // UI 갱신
        }
    }

    private void UpdateScoreUI()
    {
        string scoreText = "Score\n";
        foreach (var player in playerScores)
        {
            scoreText += $"Player {player.Key}: {player.Value.Value}\n";  // NetworkVariable.Value로 접근
        }
        UpdateScoreUIClientRpc(scoreText);  // 클라이언트에서 UI 업데이트
    }

    [ClientRpc]
    private void UpdateScoreUIClientRpc(string scoreText)
    {
        if (scoreBoardText != null)
            scoreBoardText.text = scoreText;
    }

    private void DetermineWinner()
    {

        ulong winnerId = ulong.MaxValue; 
        int maxScore = 0;

        foreach (var player in playerScores)
        {
            if (player.Value.Value > maxScore)
            {
                maxScore = player.Value.Value;
                winnerId = player.Key;
            }
        }

       

       
        DestroyFruitsServerRpc();
        EndGameServerRpc(winnerId);
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void DestroyFruitsServerRpc()
    {
        GameObject[] fruits = GameObject.FindGameObjectsWithTag("Fruit");

        foreach (GameObject fruit in fruits)
        {
            NetworkObject fruitNetworkObject = fruit.GetComponent<NetworkObject>();
            if (fruitNetworkObject != null && fruitNetworkObject.IsSpawned)
            {
                fruitNetworkObject.Despawn(); // 네트워크에서 제거
            }
            Destroy(fruit); // 게임 오브젝트 삭제
        }
    }
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
            ClearBasketObjects();
        }
    }

    private void ClearBasketObjects()
    {
        foreach (var obj in basketObjects)
        {
            if (obj != null)
            {
                Destroy(obj); // 생성된 오브젝트 삭제
            }
        }
        basketObjects.Clear(); // 리스트 초기화
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
        NetworkManager.Singleton.SceneManager.UnloadScene(SceneManager.GetSceneByName("BasketGame"));
        MinigameManager.Instance.EndMinigame();
    }
}
