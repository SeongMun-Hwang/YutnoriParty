using UnityEngine;
using Unity.Netcode;
using TMPro;
using System.Collections;

public class BasketGameManager : NetworkBehaviour
{
    [SerializeField] private float gameTime = 60f; // 게임 시간
    [SerializeField] private TextMeshProUGUI timerText; // 타이머 UI
    [SerializeField] private TextMeshProUGUI winnerText; // 승자 텍스트 UI
    [SerializeField] private GameObject winnerTextCanvas;
    private float remainingTime;
    private bool gameEnded = false;
    //게임카운트다운추가,스폰에 게임시작,끝알림추가
    private void Start()
    {
        if (IsServer)
        {
            remainingTime = gameTime;
            StartCoroutine(GameTimer());
        }
        if (winnerTextCanvas != null)
        {
            winnerTextCanvas.SetActive(false);
        }
    }


    private IEnumerator GameTimer()
    {
        while (remainingTime > 0 && !gameEnded)
        {
            yield return new WaitForSeconds(1f);
            remainingTime--;
            UpdateTimerUIClientRpc(remainingTime);
        }

        EndGame();
    }

    [ClientRpc]
    private void UpdateTimerUIClientRpc(float time)
    {
        timerText.text = $"Time: {time}s";
    }

    private void EndGame()
    {
        gameEnded = true;
        // 승자 결정 및 UI 업데이트
        var winner = GetWinner();
        DisplayWinnerClientRpc(winner);
    }

    private string GetWinner()
    {
        int maxScore = 0;
        string winnerName = "";

        foreach (var client in NetworkManager.Singleton.ConnectedClients)
        {
            var playerObject = client.Value.PlayerObject;
            var basket = playerObject.transform.Find("Basket")?.GetComponent<BasketScore>(); // 바스켓 찾기

            if (basket != null)
            {
                int playerScore = basket.GetScore();  // 플레이어의 점수 가져오기
                Debug.Log($"플레이어 {client.Value.PlayerObject.name}의 점수: {playerScore}"); // 점수 출력

                if (playerScore > maxScore)
                {
                    maxScore = playerScore;
                    winnerName = client.Value.PlayerObject.GetComponent<NetworkObject>().OwnerClientId.ToString(); // 바스켓을 소유한 플레이어의 이름 저장
                }
            }
        }

        Debug.Log($"최종 승자: {winnerName} (점수: {maxScore})"); // 최종 승자와 점수 출력
        return winnerName;
    }


    [ClientRpc]
    private void DisplayWinnerClientRpc(string winnerName)
    {
        if (winnerTextCanvas != null)
        {
            winnerTextCanvas.SetActive(true); // 승자 UI 활성화
        }

        if (winnerText != null)
        {
            winnerText.text = $"{winnerName} Win!"; // 승자 이름 출력
        }
    }
}