using Unity.Netcode;
using UnityEngine;

public class BlackHoleNode : EventNode
{
    
    [Rpc(SendTo.Server)]
    public override void EventStartRpc()
    {
        //아무도 안밟고 있으면 일 없음
        if (enteredPlayers.Count == 0)
        {
            Debug.Log("블랙홀 노드 아무도 안밟음");
            return;
        }

        //foreach(var player in enteredPlayers)
        //{
        //    Debug.Log(player.OwnerClientId + "번 플레이어 블랙홀 밟음");
        //}

        Debug.Log("블랙홀 밟음");
        DeactiveNodeRpc();
    }
}
