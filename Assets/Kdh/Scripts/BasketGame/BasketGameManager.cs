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
    [SerializeField] private TextMeshProUGUI winnerText;
    [SerializeField] private GameObject basketPrefab;

    private Dictionary<ulong, NetworkVariable<int>> playerScores = new Dictionary<ulong, NetworkVariable<int>>(); // 개별 플레이어 점수 관리
    public bool gameStarted = false;
    public bool gameEnded = false;
    private float remainingTime;
    public static BasketGameManager Instance { get; private set; }
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;  // 인스턴스를 설정
        }
        else
        {
            Destroy(gameObject);  // 이미 인스턴스가 있으면 자기 자신을 파괴
        }
    }
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            gameEnded = false;
            remainingTime = gameDuration;
            InitializePlayerScores();
            StartCoroutine(StartCountdown());
            AssignBasketsToPlayersServerRpc();
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
    [ServerRpc(RequireOwnership = false)]
    private void AssignBasketsToPlayersServerRpc()
    {
        foreach (var client in NetworkManager.Singleton.ConnectedClients)
        {
            if (client.Value.PlayerObject.TryGetComponent(out BasketGameController player))
            {
                // 1. 바구니 인스턴스 생성 (플레이어 위치에 배치)
                GameObject basketInstance = Instantiate(basketPrefab, player.transform.position + new Vector3(0, 2, 0), Quaternion.identity);

                // 2. 네트워크 오브젝트 활성화
                NetworkObject basketNetworkObject = basketInstance.GetComponent<NetworkObject>();
                basketNetworkObject.SpawnWithOwnership(player.OwnerClientId);

                // 3. 부모 변경은 클라이언트에서 실행
                SetParentClientRpc(basketNetworkObject.NetworkObjectId, player.NetworkObjectId);
            }
        }
    }

    [ClientRpc]
    private void SetParentClientRpc(ulong basketId, ulong playerId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(basketId, out NetworkObject basket) &&
            NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(playerId, out NetworkObject player))
        {
            basket.transform.SetParent(player.transform, false);
            basket.transform.localPosition = new Vector3(0, 2, 0);//플레이어에 0,2,0고정배치
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
        gameStarted = true;
        if (scoreBoardText != null)
        {
            scoreBoardText.transform.parent.gameObject.SetActive(true);  
        }

        foreach (var client in NetworkManager.Singleton.ConnectedClients)
        {
            if (client.Value.PlayerObject.TryGetComponent(out BasketGameController player))
            {
                player.EnableControl(true);
            }
        }
    }

    private IEnumerator GameTimer()
    {
        while (remainingTime > 0 && !gameEnded)
        {
            yield return new WaitForSeconds(1f);
            remainingTime--;
            UpdateRemainingTimeClientRpc((int)remainingTime);
        }

        if (!gameEnded)
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
        if (gameEnded) return;

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
        gameEnded = true;
        gameStarted = false;
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

       
        if (winnerId != ulong.MaxValue)
        {
            ShowWinnerClientRpc(winnerId);
        }
        DestroyBasketsServerRpc();
        DestroyFruitsServerRpc();
        StartCoroutine(EndGame());
    }
    [ServerRpc(RequireOwnership = false)]
    private void DestroyBasketsServerRpc()
    {
        foreach (var client in NetworkManager.Singleton.ConnectedClients)
        {
            if (client.Value.PlayerObject.TryGetComponent(out BasketGameController player))
            {
                foreach (Transform child in player.transform)
                {
                    if (child.CompareTag("Basket")) // 바구니인지 확인
                    {
                        NetworkObject basketNetworkObject = child.GetComponent<NetworkObject>();
                        if (basketNetworkObject != null && basketNetworkObject.IsSpawned)
                        {
                            basketNetworkObject.Despawn(); // 네트워크에서 제거
                        }
                        Destroy(child.gameObject); // 게임 오브젝트 삭제
                    }
                }
            }
        }
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
    [ClientRpc]
    private void ShowWinnerClientRpc(ulong winnerId)
    {
        if (winnerTextCanvas != null)
            winnerTextCanvas.SetActive(true);

        if (winnerText != null)
            winnerText.text = $"Player {winnerId} Win!";
    }

    private IEnumerator EndGame()
    {
        yield return new WaitForSeconds(3f);

        if (IsServer)
        {
            NetworkManager.Singleton.SceneManager.LoadScene("MainGame", LoadSceneMode.Single);
        }
    }
}
