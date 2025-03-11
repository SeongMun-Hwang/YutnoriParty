using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Unity.Netcode;
using UnityEngine;

public class BlackHoleNode : EventNode
{
    [HideInInspector] public List<Node> nodes;
    [SerializeField] BlackHoleTargetNode targetNodePrefab;
    List<BlackHoleTargetNode> targetNodes = new List<BlackHoleTargetNode>();

    int turnToPause = 3; //밟고 나면 비활성화 될 턴
    //float moveSpeed = 1f;
    float moveTime = 1f;
    //float timeoutSecodns = 10f;
    //int pausedTurn = 0; //밟아서 비활성화 된 턴을 기록
    //bool isPaused = false;
    WaitForFixedUpdate waitForFixedUpdate = new WaitForFixedUpdate();

    NetworkVariable<bool> isPaused = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone);
    NetworkVariable<int> pausedTurn = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone);
    NetworkVariable<int> characterCount =new NetworkVariable<int>(0,NetworkVariableReadPermission.Everyone);

    public override void OnNetworkSpawn()
    {
        minNode = data.minNode;
        maxNode = data.maxNode;
        lifeTime = data.lifeTime;

        //타겟 노드들 스폰 및 리스트 추가
        foreach (var n in nodes)
        {
            targetNodes.Add(Instantiate(targetNodePrefab, n.transform.position, Quaternion.identity));
            targetNodes.Last().GetComponent<NetworkObject>().Spawn();
        }
    }

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
        
        //밟아서 블랙홀 켠 캐릭터 기록해두고
        enteredPlayers[enteredPlayers.Count - 1].TryGet(out NetworkObject triggeredCharacter);

        Debug.Log("블랙홀 밟음");

        //블랙홀 휴식 켜주고
        //BlackHolePauseRpc();
        isPaused.Value = true;
        pausedTurn.Value = turnAfterSpawned.Value;
        Debug.Log("블랙홀 휴식시작");

        //캐릭터들 이동
        //MoveCharactersRpc();
        List<CharacterBoardMovement> list = new List<CharacterBoardMovement>();
        CharacterBoardMovement character;
        Vector3 targetPos;

        Debug.Log("이동 시작");
        PlayerManager.Instance.isMoving = true;

        for (int i = 0; i < targetNodes.Count; i++)
        {
            list.AddRange(targetNodes[i].GetCharacters());
        }

        characterCount.Value = list.Count;

        for (int i = 0; i < list.Count; i++)
        {
            character = list[i];
            var characterNetObj = character.GetComponent<NetworkObject>();

            //무인도에 갇힌 놈이면 탈출시켜줌
            if (character.GetComponent<CharacterInfo>().inIsland.Value)
            {
                EventNodeManager.Instance.EscapeIslandCallRpc(characterNetObj);
            }

            //노드 목적지로 설정
            targetPos = node.transform.position;
            targetPos.y = character.transform.position.y;

            //각 클라이언트가 본인 말 이동 시작
            MoveOwnedCharacterRpc(characterNetObj, targetPos, RpcTarget.Single(characterNetObj.OwnerClientId, RpcTargetUse.Temp));
        }

        //모든 말 이동이 끝날때까지 대기
        StartCoroutine(WaitForMoveEnd(triggeredCharacter));
    }

    IEnumerator WaitForMoveEnd(NetworkObject triggeredCharacter)
    {
        int timeOut = 10;

        while (characterCount.Value > 0)
        {
            if(timeOut <= 0)
            {
                Debug.Log("블랙홀 이동 타임아웃");
                break;
            }
            Debug.Log("블랙홀 이동처리 기다리는 중... 타임아웃까지 : " +  timeOut);
            yield return new WaitForSecondsRealtime(1);
            timeOut--;
        }

        if (characterCount.Value <= 0)
        {
            //이동 종료 후 처리
            PlayerManager.Instance.isMoving = false;
            //Debug.Log("블랙홀 쉴텐데? " + isPaused.Value);
            //블랙홀 밟은 애가 엔드 무브 실행
            BlackHoleMoveEndRpc(RpcTarget.Single(triggeredCharacter.OwnerClientId, RpcTargetUse.Temp));
            Debug.Log("전체 이동 끝");
        }
        yield return null;
    }

    [Rpc(SendTo.SpecifiedInParams)]
    void BlackHoleMoveEndRpc(RpcParams rpcParams)
    {
        Debug.Log("엔드 무브 실행할거임 : " +  NetworkManager.Singleton.LocalClientId);
        GameManager.Instance.mainGameProgress.EndMove();
    }

    [Rpc(SendTo.SpecifiedInParams)]
    void MoveOwnedCharacterRpc(NetworkObjectReference characterRef, Vector3 targetPos, RpcParams rpcParams)
    {
        if(!characterRef.TryGet(out NetworkObject characterNetObj))
        {
            Debug.Log("네트워크 오브젝트 못찾음");
        }
        Debug.Log(characterNetObj.NetworkObjectId + "번 캐릭터 이동 준비");

        var character = characterNetObj.GetComponent<CharacterBoardMovement>();
        StartCoroutine(MoveCharacter(character, targetPos));
    }

    IEnumerator MoveCharacter(CharacterBoardMovement character, Vector3 targetPos)
    {
        Debug.Log("이동 캐릭터 id : " + character.GetComponent<NetworkObject>().NetworkObjectId);

        Vector3 velocity = Vector3.zero;
        Animator animator = character.GetComponent<Animator>();
        animator.SetFloat("isMoving", 1f); //걷기 애니메이션 시작

        while (Vector3.Distance(character.transform.position, targetPos) > 0.01f)
        {
            character.transform.position = Vector3.SmoothDamp(character.transform.position, targetPos, ref velocity, moveTime);
            yield return waitForFixedUpdate;
        }

        character.transform.position = targetPos;

        //이동 끝나고 노드 변경
        character.ChangeCurrentNode(node);
        animator.SetFloat("isMoving", 0f);

        //캐릭터 카운트는 서버에서밖에 못건듬
        ChangeCharacterCountRpc();

        Debug.Log(character.GetComponent<NetworkObject>().NetworkObjectId + "번 캐릭터 이동 끝");
        yield return null;
    }

    [Rpc(SendTo.Server)]
    void ChangeCharacterCountRpc()
    {
        characterCount.Value--;
        Debug.Log("캐릭터 카운트 : " + characterCount.Value);
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
        Debug.Log("블랙홀 휴식시작");
    }

    [Rpc(SendTo.Server)]
    void BlackHoleUnpauseRpc()
    {
        isPaused.Value = false;
        pausedTurn.Value = 0;
        Debug.Log("블랙홀 휴식끝");
    }
}
