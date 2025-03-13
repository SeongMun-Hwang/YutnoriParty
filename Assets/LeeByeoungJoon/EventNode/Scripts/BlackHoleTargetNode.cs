using System.Collections.Generic;
using Unity.Netcode;

public class BlackHoleTargetNode : EventNode
{
    public override void OnNetworkSpawn()
    {
        //이벤트 노드 데이터 초기화 무시하려고 빈 함수 넣어둠
        //Debug.Log("블랙홀 타겟 생성");
    }

    //이 노드를 밟고 있는 캐릭터들 리스트를 리턴
    public List<CharacterBoardMovement> GetCharacters()
    {
        List<CharacterBoardMovement> list = new List<CharacterBoardMovement>();

        foreach(var player in enteredPlayers)
        {
            if(!player.TryGet(out NetworkObject no))
            {
                return null;
            }
            list.Add(no.GetComponent<CharacterBoardMovement>());
        }

        return list;
    }
}
