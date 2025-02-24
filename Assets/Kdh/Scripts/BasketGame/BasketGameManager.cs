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

    private Dictionary<ulong, NetworkVariable<int>> playerScores = new Dictionary<ulong, NetworkVariable<int>>(); // 개별 플레이어 점수 관리
    private bool gameStarted = false;
    private bool gameEnded = false;
    private float remainingTime;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            gameEnded = false;
            remainingTime = gameDuration;
            InitializePlayerScores();
            StartCoroutine(StartCountdown());
        }
    }

    private void InitializePlayerScores()
    {
        foreach (var client in NetworkManager.Singleton.ConnectedClients)
        {
            ulong clientId = client.Key;
            playerScores[clientId] = new NetworkVariable<int>(0);
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
        gameStarted = true;

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
                remainingTimeText.text = $"남은 시간: {time}초";
            else
                remainingTimeText.text = "게임 종료!";
        }
    }

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
        string scoreText = "점수 현황\n";
        foreach (var player in playerScores)
        {
            scoreText += $"Player {player.Key}: {player.Value.Value}점\n";  // NetworkVariable.Value로 접근
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

        ulong winnerId = ulong.MaxValue; // 기본값을 `ulong.MaxValue`로 설정
        int maxScore = 0;

        foreach (var player in playerScores)
        {
            if (player.Value.Value > maxScore)
            {
                maxScore = player.Value.Value;
                winnerId = player.Key;
            }
        }

        // **승자가 정상적으로 설정되었는지 확인 후 UI 업데이트**
        if (winnerId != ulong.MaxValue)
        {
            ShowWinnerClientRpc(winnerId);
        }

        StartCoroutine(EndGame());
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
