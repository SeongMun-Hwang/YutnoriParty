using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class BlockSpawnHandler : NetworkBehaviour
{
	// 게임 엔티티 관련
	[SerializeField] private GameObject blockPrefab;
	[SerializeField] private GameObject bottomFrame;
	private NetworkList<NetworkObjectReference> stack = new NetworkList<NetworkObjectReference>();

	// 색상 관련
	[SerializeField] private List<Color32> teamColors;
	private int colorIndex;

	public override void OnNetworkSpawn()
	{
		if (IsServer)
		{
			stack.Clear();
			CreateBlock();
		}
	}

	public void CreateBlock()
	{
		if (!IsServer) return; // 서버에서만 블록 생성

		GameObject activeTile;
		Vector3 spawnPosition;

		if (stack.Count > 0) // 기존 블록이 있을 경우
		{
			if (stack[stack.Count - 1].TryGet(out NetworkObject previousTileNetObj))
			{
				GameObject previousTile = previousTileNetObj.gameObject;
				spawnPosition = new Vector3(
					previousTile.transform.position.x,
					previousTile.transform.position.y + previousTile.transform.localScale.y,
					previousTile.transform.position.z);
			}
			else
			{
				spawnPosition = bottomFrame.transform.position;
			}
		}
		else // 첫 번째 블록 생성
		{
			spawnPosition = bottomFrame.transform.position + Vector3.up;
		}

		// 블록 생성 및 네트워크에 등록
		activeTile = Instantiate(blockPrefab, spawnPosition, Quaternion.identity, transform.parent);
		StackableBlock stackableBlock = activeTile.GetComponent<StackableBlock>();
		stackableBlock.spawner = this;

		stackableBlock.moveX = stack.Count % 2 == 0;

		NetworkObject netObj = activeTile.GetComponent<NetworkObject>();
		netObj.Spawn(true); // 네트워크에 생성 등록
		stack.Add(netObj);  // NetworkList에 추가

		// 크기 및 색상 적용
		if (stack.Count > 1)
		{
			if (stack[stack.Count - 2].TryGet(out NetworkObject prevNetObj))
			{
				activeTile.transform.localScale = prevNetObj.gameObject.transform.localScale;
			}
		}

		colorIndex = (colorIndex + 1) % teamColors.Count;
		activeTile.GetComponent<Renderer>().material.color = teamColors[colorIndex];
	}

	public void DropBlock()
	{
		if (!IsServer) return; // 서버에서만 블록 배치
		if (stack.Count > 0)
		{
			if (stack[stack.Count - 1].TryGet(out NetworkObject currentNetBlock))
			{
				GameObject currentBlock = currentNetBlock.gameObject;
				currentBlock.GetComponent<StackableBlock>().ScaleBlockServerRpc();
			}
		}

		CreateBlock();
	}
}