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
    [SerializeField] private GameObject winnerTextCanvas; 
    [SerializeField] private TextMeshProUGUI winnerText; 

    private bool gameStarted = false;
    private bool gameEnded = false; 
    private ulong winnerClientId;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            gameEnded = false; // 씬에 들어올 때 게임 종료 상태 초기화
            StartCoroutine(StartCountdown());
        }
        if (gameStarted && IsClient)
        {
            EnablePlayerControl();
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
            if (client.Value.PlayerObject.TryGetComponent(out RunGameController player))
            {
                player.EnableControl(true);
            }
        }

        FindAnyObjectByType<Chase>().EnableChase();
    }
    private void EnablePlayerControl()
    {
        // 미니게임 씬에 들어갔을 때 플레이어 컨트롤 활성화
        if (TryGetComponent(out RunGameController playerController))
        {
            playerController.EnableControl(true);
        }
    }

    private void Update()
    {
        if (!gameStarted || !IsServer || gameEnded) return;

        if (NetworkManager.Singleton.ConnectedClients.Count >= 2)
        {
            CheckForWinner();
            CheckRemainingPlayers();
        }
    }

    private void CheckForWinner()
    {
        if (gameEnded) return;

        foreach (var client in NetworkManager.Singleton.ConnectedClients)
        {
            var playerObject = client.Value.PlayerObject;

            if (playerObject.TryGetComponent(out RunGameController playerController) && !playerController.IsEliminated)
            {
                if (goalArea.bounds.Contains(playerController.transform.position))
                {
                    EndGame(playerController.OwnerClientId);
                    return;
                }
            }
        }
    }

    public void CheckRemainingPlayers()
    {
        if (gameEnded) return;

        List<RunGameController> alivePlayers = new List<RunGameController>();

        foreach (var client in NetworkManager.Singleton.ConnectedClients)
        {
            var playerObject = client.Value.PlayerObject;

            if (playerObject.TryGetComponent(out RunGameController playerController) && !playerController.IsEliminated)
            {
                alivePlayers.Add(playerController);
            }
        }

        if (alivePlayers.Count == 1)
        {
            EndGame(alivePlayers[0].OwnerClientId);
        }
    }

    private void EndGame(ulong winnerId)
    {
        gameEnded = true;
        winnerClientId = winnerId;

        Debug.Log($"Player {winnerId} 이 승리했습니다!");
        StopAllPlayersClientRpc();
        ShowWinnerClientRpc(winnerId);

        Debug.Log("미니게임이 종료되었습니다.");
        StartCoroutine(LoadNextScene());
    }

    [ClientRpc]
    private void ShowWinnerClientRpc(ulong winnerId)
    {
        if (winnerTextCanvas != null)
            winnerTextCanvas.SetActive(true);

        if (winnerText != null)
            winnerText.text = $"Player {winnerId} Win!";
    }
    [ClientRpc]
    private void StopAllPlayersClientRpc()
    {
        foreach (var client in NetworkManager.Singleton.ConnectedClients)
        {
            if (client.Value.PlayerObject.TryGetComponent(out RunGameController player))
            {
                player.EnableControl(false); // 조작 비활성화
            }
        }
    }

    [ClientRpc]
    private void EndGame_ClientRpc(ulong networkObjectId)
    {
        var playerObject = NetworkManager.Singleton.SpawnManager.SpawnedObjects[networkObjectId];

        if (playerObject != null && playerObject.TryGetComponent(out RunGameController playerController))
        {
            playerController.EnableControl(false);
        }
    }

    private IEnumerator LoadNextScene()
    {
        yield return new WaitForSeconds(3f);

        if (IsServer)
        {
            NetworkManager.Singleton.SceneManager.LoadScene("MainGame", LoadSceneMode.Single); // 모든 클라이언트가 이동
            EnableAllPlayersControlClientRpc(); // 씬 이동 후 즉시 실행
        }
    }

    [ClientRpc]
    private void EnableAllPlayersControlClientRpc()
    {
        foreach (var client in NetworkManager.Singleton.ConnectedClients)
        {
            if (client.Value.PlayerObject.TryGetComponent(out RunGameController player))
            {
                player.SetEliminated(false);
                player.EnableControl(true);
            }
        }
    }

}