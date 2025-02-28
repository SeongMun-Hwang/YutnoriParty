using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class IslandNode : EventNode
{
    Dictionary<NetworkObject, int> trappedPlayers = new Dictionary<NetworkObject, int>();
    int trapTurn = 2;

    [Rpc(SendTo.Server)]
    public override void EventStartRpc()
    {
        //아무도 안밟고 있으면 일 없음
        if (enteredPlayers.Count == 0)
        {
            Debug.Log("무인도 노드 아무도 안밟음");
            return;
        }

        //서버에서 무인도에 갇힌 캐릭터들 판정
        TrapPlayersRpc();


    }

    [Rpc(SendTo.Server)]
    void TrapPlayersRpc()
    {
        Debug.Log("무인도 밟음");

        //무인도 노드 안에 들어와 있는 모든 캐릭터들한테 적용
        foreach(var player in enteredPlayers)
        {
            //딕셔너리에 없는 애들은 추가해주고
            if (!trappedPlayers.ContainsKey(player))
            {
                trappedPlayers.Add(player, trapTurn);
            }
            //딕셔너리에 들어와있으면 캐릭터 이동 못하게 한다 == 선택을 못하게 함
            else
            {
                //남은 턴 0이하 되면 탈출
                if (trappedPlayers[player] <= 0)
                {
                    Debug.Log("탈출");
                }

                BlockMoveRpc(enteredPlayers.IndexOf(player), RpcTarget.Single(player.OwnerClientId, RpcTargetUse.Temp));
                
                //한턴씩 깎아줌
                trappedPlayers[player]--;
            }
        }
    }

    //이동 못하게 함, 특정 클라이언트만 실행
    [Rpc(SendTo.SpecifiedInParams)]
    void BlockMoveRpc(int idx, RpcParams rpcParams)
    {
        var player = enteredPlayers[idx];
        //지금 플레이하려는 캐릭터랑 무인도에 갇힌 캐릭터가 같으면 currentCharacter를 비워버린다
        if (player.GetComponent<CharacterBoardMovement>() == GameManager.Instance.mainGameProgress.currentCharacter)
        {
            Debug.Log("무인도에 갇힌 캐릭임");
            GameManager.Instance.mainGameProgress.currentCharacter = null;
        }
    }
}
