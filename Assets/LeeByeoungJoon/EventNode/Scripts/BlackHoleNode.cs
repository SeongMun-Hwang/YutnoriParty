using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    NetworkVariable<bool> isProcessing = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone);

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
        EventExcuteRpc();

        //휴식중인데 밟으면 바로 리턴
        if(isPaused.Value)
        {
            //이미 작동중이면 패스
            if (isProcessing.Value)
            {
                return;
            }

            Debug.Log("블랙홀 휴식중");
            BlackHoleEventEndRpc();
            return;
        }
        //아무도 안밟고 있으면 일 없음
        if (enteredPlayers.Count == 0)
        {
            Debug.Log("블랙홀 노드 아무도 안밟음");
            BlackHoleEventEndRpc();
            return;
        }

        isProcessing.Value = true;
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
        List<ulong> playerIds = new List<ulong>();
        List<NetworkObjectReference> characterGameobjects = new List<NetworkObjectReference>();
        CharacterBoardMovement character;
        Vector3 targetPos;

        Debug.Log("이동 시작");
        //PlayerManager.Instance.isMoving = true;

        //밟은애도 사라져야하니까 목록에 추가
        playerIds.Add(triggeredCharacter.OwnerClientId);
        characterGameobjects.Add(triggeredCharacter);

        for (int i = 0; i < targetNodes.Count; i++)
        {
            list.AddRange(targetNodes[i].GetCharacters());
        }

        characterCount.Value = list.Count;

        for (int i = 0; i < list.Count; i++)
        {
            character = list[i];
            characterGameobjects.Add(character.gameObject);

            var characterNetObj = character.GetComponent<NetworkObject>();

            //무인도에 갇힌 놈이면 탈출시켜줌
            if (character.GetComponent<CharacterInfo>().inIsland.Value)
            {
                EventNodeManager.Instance.EscapeIslandCallRpc(characterNetObj);
            }

            //플레이어 아이디 리스트에 없으면 추가
            if (!playerIds.Contains(characterNetObj.OwnerClientId))
            {
                playerIds.Add(characterNetObj.OwnerClientId);
            }

            //노드 목적지로 설정
            targetPos = node.transform.position;
            targetPos.y = character.transform.position.y;

            //각 클라이언트가 본인 말 이동 시작
            MoveOwnedCharacterRpc(characterNetObj, targetPos, RpcTarget.Single(characterNetObj.OwnerClientId, RpcTargetUse.Temp));
        }

        //모든 말 이동이 끝날때까지 대기
        StartCoroutine(WaitForMoveEnd(triggeredCharacter, playerIds, characterGameobjects));
    }

    //서버에서 실행
    IEnumerator WaitForMoveEnd(NetworkObject triggeredCharacter, List<ulong> playerIds ,List<NetworkObjectReference> characters)
    {
        int timeOut = 10;

        while (characterCount.Value > 0)
        {
            //if(timeOut <= 0)
            //{
            //    Debug.Log("블랙홀 이동 타임아웃");
            //    break;
            //}
            //Debug.Log("블랙홀 이동처리 기다리는 중... 타임아웃까지 : " +  timeOut);
            Debug.Log("블랙홀 이동처리 기다리는 중...");

            yield return new WaitForSecondsRealtime(1);
            timeOut--;
        }

        //캐릭터들 이동이 모두 끝나고, 플레이어가 둘 이상일때만 미니게임 시작
        if (characterCount.Value <= 0 && playerIds.Count >= 2)
        {
            //이동 종료 후 처리
            //Debug.Log("블랙홀 쉴텐데? " + isPaused.Value);
            //블랙홀 밟은 애가 엔드 무브 실행
            //BlackHoleMoveEndRpc(RpcTarget.Single(triggeredCharacter.OwnerClientId, RpcTargetUse.Temp));

            //미니게임 다 했다는 콜 받기 위해 블랙홀 노드 전달
            GameManager.Instance.mainGameProgress.StartBlackHoleMiniGameServerRpc(gameObject, triggeredCharacter, playerIds.ToArray(), characters.ToArray());
        }
        else
        {
            BlackHoleEventEndRpc();
        }

        Debug.Log("전체 이동 끝");

        yield return null;
    }
    [Rpc(SendTo.Server)]
    public void BlackHoleEventEndRpc()
    {
        isProcessing.Value = false;
        EventEndRpc();
    }

    [Rpc(SendTo.SpecifiedInParams)]
    void BlackHoleMoveEndRpc(RpcParams rpcParams)
    {
        Debug.Log("엔드 무브 실행할거임 : " +  NetworkManager.Singleton.LocalClientId);
        //StartCoroutine(GameManager.Instance.mainGameProgress.EndMove());
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
        character.ChangeCurrentNodeToBlackHole();
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
