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
        EventExcuteRpc();
        //아무도 안밟고 있으면 일 없음
        if (enteredPlayers.Count == 0)
        {
            Debug.Log("무인도 노드 아무도 안밟음");
            EventEndRpc();
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
        foreach(var playerReference in enteredPlayers)
        {
            if (!playerReference.TryGet(out NetworkObject player))
            {
                return;
            }

            //딕셔너리에 없는 애들은 추가해주고
            if (!trappedPlayers.ContainsKey(player))
            {
                trappedPlayers.Add(player, trapTurn);
            }

            //남은 턴 0이하 되면 탈출
            if (trappedPlayers[player] <= 0)
            {
                EscapeIslandRpc(player);
                continue;
            }

            //못움직이게 막음
            player.GetComponent<CharacterInfo>().canMove.Value = false;
            player.GetComponent<CharacterInfo>().inIsland.Value = true;

            //SetCharacterMoveRpc(false, enteredPlayers.IndexOf(player), RpcTarget.Single(player.OwnerClientId, RpcTargetUse.Temp));
            //BlockMoveRpc(enteredPlayers.IndexOf(player), RpcTarget.Single(player.OwnerClientId, RpcTargetUse.Temp));

            Debug.Log("남은 턴 : " + trappedPlayers[player]);
        }

        EventEndRpc();
    }

    [Rpc(SendTo.Server)]
    public override void TurnIncreaseRpc()
    {
        turnAfterSpawned.Value++;

        //남은 턴 감소
        foreach (var player in enteredPlayers)
        {
            if (!trappedPlayers.ContainsKey(player))
            {
                Debug.Log("딕셔너리에 없음");
                return;
            }
            if(trappedPlayers[player] > 0)
            {
                trappedPlayers[player]--;
            }
        }
    }

    [Rpc(SendTo.Server)]
    public void EscapeIslandRpc(NetworkObjectReference no)
    {
        if(!no.TryGet(out NetworkObject player))
        {
            Debug.Log("네트워크 오브젝트 없음");
            return;
        }
        
        //딕셔너리에 없으면 걍 스킵
        if (!trappedPlayers.ContainsKey(player))
        {
            Debug.Log("딕셔너리에 없음");
            return;
        }

        player.GetComponent<CharacterInfo>().canMove.Value = true;
        player.GetComponent<CharacterInfo>().inIsland.Value = false;
        trappedPlayers.Remove(player); //딕셔너리에서 지워도 인덱스 문제 안나겠지..?
        //Debug.Log(player.NetworkObjectId + " 탈출");
    }

    //이동 못하게 함, 특정 클라이언트만 실행
    [Rpc(SendTo.SpecifiedInParams)]
    void BlockMoveRpc(int idx, RpcParams rpcParams)
    {
        if(!enteredPlayers[idx].TryGet(out NetworkObject player))
        {
            return;
        }
        //지금 플레이하려는 캐릭터랑 무인도에 갇힌 캐릭터가 같으면 currentCharacter를 비워버린다
        if (player.GetComponent<CharacterBoardMovement>() == GameManager.Instance.mainGameProgress.currentCharacter)
        {
            Debug.Log("무인도에 갇힌 캐릭임");
            GameManager.Instance.mainGameProgress.currentCharacter = null;
        }
    }

    [Rpc(SendTo.Server)]
    void SetCharacterMoveRpc(bool canMove, int idx, RpcParams rpcParams)
    {
        if (!enteredPlayers[idx].TryGet(out NetworkObject player))
        {
            return;
        }
        player.GetComponent<CharacterInfo>().canMove.Value = canMove;
    }
}
