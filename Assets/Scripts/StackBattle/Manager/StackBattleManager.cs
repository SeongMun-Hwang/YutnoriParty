using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// 참가자 정보
[Serializable]
public class PlayerProfile
{
	public string Name;
	public int Score;
}

public class StackBattleManager : MonoBehaviour
{
	// 게임에 참여하는 유저 관련
	[SerializeField] private int maxPlayers;
	[SerializeField] private List<PlayerProfile> players;

	// 현재 차례 플레이어의 인덱스
	[SerializeField] private int currentPlayerIndex;

	// 게임 블록 관련
	[SerializeField] private BlockSpawnHandler blockSpawnHandler;

	// 게임 UI 관련
	[SerializeField] private List<ProfileView> profileViewList;
	[SerializeField] private TMP_Text orderUI;

	// 게임 상태 관련
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

	// 게임의 데이터를 초기화
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
