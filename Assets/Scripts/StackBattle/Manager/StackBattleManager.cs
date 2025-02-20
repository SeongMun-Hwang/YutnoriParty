using System;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
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

	// UI관련
	public TMP_Text turnText;
	public Button turnButton;
	public BlockSpawnHandler spawner;

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
		UpdateTurnUI(0, GetCurrentTurnPlayerId());
	}

	public override void OnNetworkSpawn()
	{
		if (IsServer)
		{
			NetworkManager.Singleton.OnClientConnectedCallback += OnPlayerJoined;
		}
	}

	private void OnPlayerJoined(ulong clientId)
	{
		if (!playerIds.Contains(clientId))
		{
			playerIds.Add(clientId);
			isRetire.Add(false);
		}

		// 첫 번째 플레이어가 게임 시작 시 첫 턴을 가짐
		if (playerIds.Count == 1)
		{
			currentTurnPlayerId.Value = playerIds[0];
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void RequestNextTurnServerRpc(ulong senderClientId)
	{
		if (currentTurnPlayerId.Value == senderClientId)
		{
			spawner.DropBlock();
			int currentIndex = playerIds.IndexOf(senderClientId);
			int nextIndex = (currentIndex + 1) % playerIds.Count;

			if (isRetire.Contains(false))
			{
				while (isRetire[nextIndex])
				{
					nextIndex = (nextIndex + 1) % playerIds.Count;
				}

				currentTurnPlayerId.Value = playerIds[nextIndex]; // 턴 넘김
			
				spawner.CreateBlock();
			}
			else
			{
				Debug.Log("전원 탈락");
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
			Debug.Log(isRetire[playerIds.IndexOf(NetworkManager.Singleton.LocalClientId)]);
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
	}
}
