using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.TextCore.Text;

public class ItemNode : EventNode
{
    [Rpc(SendTo.Server)]
    public override void EventStartRpc()
    {
        //아무도 안밟고 있으면 일 없음
        if (enteredPlayers.Count == 0)
        {
            Debug.Log("아이템 노드 아무도 안밟음");
            return;
        }

        if (!enteredPlayers[enteredPlayers.Count - 1].TryGet(out NetworkObject character))
        {
            Debug.Log("네트워크 오브젝트 없음");
            return;
        }

        Debug.Log(character.OwnerClientId + "플레이어 아이템 획득");
        ItemManager.Instance.GetItemClientRpc(character.OwnerClientId);
        DeactiveNodeRpc();
    }
}