using System.Globalization;
using Unity.Netcode;
using UnityEngine;

public class PlayerSlot : NetworkBehaviour
{
	public override void OnNetworkSpawn()
	{
		if (IsOwner) // ���� ���� �÷��̾ ����
		{
			Transform parent = GameObject.Find("Player List")?.transform;
			if (parent != null)
			{
				transform.SetParent(parent);
			}
		}
	}
}
