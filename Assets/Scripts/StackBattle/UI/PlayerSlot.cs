using System.Globalization;
using Unity.Netcode;
using UnityEngine;

public class PlayerSlot : NetworkBehaviour
{
	public override void OnNetworkSpawn()
	{
		if (IsOwner) // 현재 로컬 플레이어만 실행
		{
			Transform parent = GameObject.Find("Player List")?.transform;
			if (parent != null)
			{
				transform.SetParent(parent);
			}
		}
	}
}
