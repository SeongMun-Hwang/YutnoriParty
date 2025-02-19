using System;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class StackBattleManager : NetworkBehaviour
{
	// ���ӿ� �����ϴ� ���� ����
	[SerializeField] private int maxPlayers;
	private List<ulong> playerIds = new List<ulong>(); // ������ �÷��̾� ID ����Ʈ

	// ���� ���� �÷��̾� ID
	private NetworkVariable<ulong> currentTurnPlayerId = new NetworkVariable<ulong>(0);

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
		}

		// ù ��° �÷��̾ ���� ���� �� ù ���� ����
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
			currentTurnPlayerId.Value = playerIds[nextIndex]; // �� �ѱ�
			spawner.CreateBlock();
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
	}
}
