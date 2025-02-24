using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class StackBattleManager : NetworkBehaviour
{
	// 게임에 참여하는 유저 관련
	[SerializeField] private int maxPlayers;
	private NetworkList<ulong> playerIds = new NetworkList<ulong>(); // 참가한 플레이어 ID 리스트

	// 현재 차례 플레이어 ID
	private NetworkVariable<ulong> currentTurnPlayerId = new NetworkVariable<ulong>(0);
	
	// 게임오버된 플레이어 리스트
	private NetworkList<bool> isRetire = new NetworkList<bool>();

    private int timer = 5; // 턴 제한시간
    private Coroutine turnTimerCoroutine; // 턴 제한시간 타이머 코루틴
    private bool failed = false;
    private bool timeover = false;
	public BlockSpawnHandler spawner;

	// UI관련
	[SerializeField] private TMP_Text turnText;
	[SerializeField] private TMP_Text timerUI;
	[SerializeField] private Button turnButton;
	[SerializeField] private GameObject winMessageUI;
	[SerializeField] private GameObject loseMessageUI;

	private void Start()
	{
		turnButton.onClick.AddListener(() =>
		{
			if (NetworkManager.Singleton.IsClient)
			{
				RequestNextTurnServerRpc(NetworkManager.Singleton.LocalClientId);
			}
		});

		spawner.manager = this;
		currentTurnPlayerId.OnValueChanged += UpdateButtonInteractable;
		currentTurnPlayerId.OnValueChanged += UpdateTurnUI;

        if (IsServer)
        {
            // 서버라면 턴이 바뀔 때마다 타이머 재시작
            currentTurnPlayerId.OnValueChanged += OnTurnChanged;
        }
    }

	public override void OnNetworkSpawn()
	{
		if (IsServer)
		{
			NetworkManager.Singleton.OnClientConnectedCallback += OnPlayerJoined;
		}

		turnButton.interactable = (GetCurrentTurnPlayerId() == NetworkManager.Singleton.LocalClientId);
	}

	private void OnPlayerJoined(ulong clientId)
	{
		if (!playerIds.Contains(clientId))
		{
			playerIds.Add(clientId);
			isRetire.Add(false);
		}

        if (playerIds.Count == maxPlayers)
        {
            // 첫 번째 플레이어가 게임 시작 시 첫 턴을 가짐
            currentTurnPlayerId.Value = playerIds[0];
            StartTurnTimer();
        }
	}

    private void OnTurnChanged(ulong previousValue, ulong newValue)
    {
        if (IsServer)
        {
            if (turnTimerCoroutine != null)
            {
                StopCoroutine(turnTimerCoroutine);
            }
            StartTurnTimer();
        }
    }

    private void StartTurnTimer()
    {
        timeover = false;
        turnTimerCoroutine = StartCoroutine(TurnTimerCoroutine());
    }

    private IEnumerator TurnTimerCoroutine()
    {
        int remainTime = timer;
        while (remainTime > 0)
        {
            // 클라이언트의 UI 업데이트를 위한 ClientRpc 호출
            UpdateTimerUIClientRpc(remainTime);
            yield return new WaitForSeconds(1f);
            remainTime -= 1;
        }
        // 제한 시간이 지나면 현재 턴을 자동 종료
        timeover = true;
        GameOver();
        RequestNextTurnServerRpc(currentTurnPlayerId.Value);
    }

    [ClientRpc]
    private void UpdateTimerUIClientRpc(int time)
    {
        timerUI.text = time.ToString();
    }

    [ServerRpc(RequireOwnership = false)]
	public void RequestNextTurnServerRpc(ulong senderClientId)
	{
		if (currentTurnPlayerId.Value == senderClientId)
		{
            // 턴 종료 요청 시 타이머 코루틴 정지
            if (turnTimerCoroutine != null)
            {
                StopCoroutine(turnTimerCoroutine);
                turnTimerCoroutine = null;
            }

            failed = false;
            if (!timeover)
            {
                spawner.DropBlock();
            }
			
			int currentIndex = playerIds.IndexOf(senderClientId);
			int nextIndex = (currentIndex + 1) % playerIds.Count;

			int aliveCount = 0;
			for (int i = 0; i < isRetire.Count; i++)
			{
				if (!isRetire[i]) { aliveCount++; }
			}

			if (aliveCount > 1)
			{
				while (isRetire[nextIndex])
				{
					nextIndex = (nextIndex + 1) % playerIds.Count;
				}

				currentTurnPlayerId.Value = playerIds[nextIndex]; // 턴 넘김

                if (!timeover && !failed)
				{
					spawner.CreateBlock();
				}

                // 새 턴 시작 시 타이머를 다시 시작
                StartTurnTimer();
            }
			else
			{
				int aliveIndex = isRetire.IndexOf(false);
				// Debug.Log($"게임 종료! {aliveIndex}플레이어 승리");
				GameFinishedClientRpc(playerIds[aliveIndex]);
			}
		}
	}

	public ulong GetCurrentTurnPlayerId()
	{
		return currentTurnPlayerId.Value;
	}

	private void UpdateButtonInteractable(ulong previousValue, ulong newValue)
	{
		turnButton.interactable = (newValue == NetworkManager.Singleton.LocalClientId);
	}

	private void UpdateTurnUI(ulong previousValue, ulong newValue)
	{
		turnText.text = $"Current Turn : Player {newValue}";

        if (IsClient)
		{
			// Debug.Log(isRetire[playerIds.IndexOf(NetworkManager.Singleton.LocalClientId)]);
		}
	}

    public void GameOver()
	{
		GameOverServerRpc(GetCurrentTurnPlayerId());
	}

	[ServerRpc]
	public void GameOverServerRpc(ulong id)
	{
		isRetire[playerIds.IndexOf(id)] = true;
		failed = true;

		GameOverClientRpc(id);
	}

	[ClientRpc]
	public void GameOverClientRpc(ulong id)
	{
		if (NetworkManager.Singleton.LocalClientId == id)
		{
			loseMessageUI.SetActive(true);
		}
	}
	
	[ClientRpc]
	public void GameFinishedClientRpc(ulong winClientId)
	{
		if (NetworkManager.Singleton.LocalClientId == winClientId)
		{
			winMessageUI.SetActive(true);
		}

		turnButton.interactable = false;
		Debug.Log("게임 종료");
	}
}
