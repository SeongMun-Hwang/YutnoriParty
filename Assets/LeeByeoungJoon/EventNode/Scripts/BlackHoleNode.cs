using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class BlackHoleNode : EventNode
{
    int turnToPause = 3; //밟고 나면 비활성화 될 턴
    //int pausedTurn = 0; //밟아서 비활성화 된 턴을 기록
    //bool isPaused = false;
    NetworkVariable<bool> isPaused = new NetworkVariable<bool>(false);
    NetworkVariable<int> pausedTurn = new NetworkVariable<int>(0);

    [Rpc(SendTo.Server)]
    public override void EventStartRpc()
    {
        if(isPaused.Value)
        {
            Debug.Log("블랙홀 휴식중");
            return;
        }
        //아무도 안밟고 있으면 일 없음
        if (enteredPlayers.Count == 0)
        {
            Debug.Log("블랙홀 노드 아무도 안밟음");
            return;
        }

        Debug.Log("블랙홀 밟음");
        BlackHolePauseRpc();
        //DeactiveNodeRpc();
    }
    
    [Rpc(SendTo.Server)]
    public override void TurnIncreaseRpc()
    {
        turnAfterSpawned.Value++;
        
        //비활성화 되어있고, 비활성화 유지 턴 넘었으면 다시 활성화
        if(isPaused.Value && turnAfterSpawned.Value - pausedTurn.Value > turnToPause)
        {
            BlackHoleUnpauseRpc();
        }
    }

    [Rpc(SendTo.Server)]
    void BlackHolePauseRpc()
    {
        isPaused.Value = true;
        pausedTurn.Value = turnAfterSpawned.Value;
    }

    [Rpc(SendTo.Server)]
    void BlackHoleUnpauseRpc()
    {
        isPaused.Value = false;
        pausedTurn.Value = 0;
    }
}
