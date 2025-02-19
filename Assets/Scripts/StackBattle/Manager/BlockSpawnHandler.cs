using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class BlockSpawnHandler : NetworkBehaviour
{
	// 게임 엔티티 관련
	[SerializeField] private GameObject blockPrefab;
	[SerializeField] private GameObject bottomFrame;
	[SerializeField] private List<GameObject> stack;

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
		if (!IsServer)
			return; // 서버에서만 블록 생성

		GameObject previousTile;
		GameObject activeTile;

		if (stack.Count > 0)
		{
			previousTile = stack[stack.Count - 1];
		}
		else
		{
			previousTile = bottomFrame;
		}

		activeTile = Instantiate(blockPrefab);
		stack.Add(activeTile);
		activeTile.transform.localScale = previousTile.transform.localScale;
		activeTile.transform.position = previousTile.transform.position + Vector3.up;
		activeTile.GetComponent<StackableBlock>().moveX = stack.Count % 2 == 0;
		activeTile.GetComponent<StackableBlock>().spawner = this;

		NetworkObject netObj = activeTile.GetComponent<NetworkObject>();
		netObj.Spawn(true); // 네트워크에 생성 등록
	}

	public void DropBlock()
	{
		if (!IsServer) return; // 서버에서만 블록 배치
		stack[stack.Count - 1].GetComponent<StackableBlock>().ScaleBlock();
	}
}