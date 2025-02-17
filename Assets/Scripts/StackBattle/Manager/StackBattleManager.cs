using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// ������ ����
[Serializable]
public class PlayerProfile
{
	public string Name;
	public int Score;
}

public class StackBattleManager : MonoBehaviour
{
	// ���ӿ� �����ϴ� ���� ����
	[SerializeField] private int maxPlayers;
	[SerializeField] private List<PlayerProfile> players;

	// ���� ���� �÷��̾��� �ε���
	[SerializeField] private int currentPlayerIndex;

	// ���� ��� ����
	[SerializeField] private BlockSpawnHandler blockSpawnHandler;

	// ���� UI ����
	[SerializeField] private List<ProfileView> profileViewList;
	[SerializeField] private TMP_Text orderUI;

	// ���� ���� ����
	public enum StackBattleState
	{
		Ready,
		Playing,
		Wait,
		GameOver
	}
	public StackBattleState state;

	private void OnEnable()
	{
		Init();
	}

	// ������ �����͸� �ʱ�ȭ
	private void Init()
	{
		currentPlayerIndex = 0;
		state = StackBattleState.Playing;

		orderUI.text = $"Go, {players[currentPlayerIndex].Name}!";
		for (int i = 0; i < maxPlayers; i++)
		{
			profileViewList[i].gameObject.SetActive(true);
			profileViewList[i].InitView(players[i]);
		}
	}

	private void Update()
	{
		switch (state)
		{
			case StackBattleState.Wait:
				break;
			case StackBattleState.Playing:
				if (Input.GetMouseButtonDown(0))
				{
					blockSpawnHandler.DropBlock();
					currentPlayerIndex++;
					if (currentPlayerIndex >= maxPlayers) { currentPlayerIndex = 0; }
					orderUI.text = $"Go, {players[currentPlayerIndex].Name}!";
				}
				break;
			case StackBattleState.Ready:

				break;
			case StackBattleState.GameOver:

				break;
		}
	}
}
