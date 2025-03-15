using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class EventNode : NetworkBehaviour
{
    [HideInInspector] public Node node;
    public EventNodeData data;
    public bool isCheckingTrigger = false;
    public NetworkVariable<bool> isEventRunning = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone);
    //참조만 할 정보들 => 스크립터블 오브젝트에 저장 가능
    protected int minNode = 1;
    protected int maxNode = 1;
    protected int lifeTime = 0;
    public int MinNode { get { return minNode; } }
    public int MaxNode { get {return maxNode; } }

    //실시간으로 바뀌어야 할 정보들
    protected NetworkVariable<int> turnAfterSpawned = new NetworkVariable<int>(0);
    protected NetworkObject enteredPlayer;
    protected NetworkObject exitPlayer;
    //protected List<NetworkObject> enteredPlayers = new List<NetworkObject>();
    protected NetworkList<NetworkObjectReference> enteredPlayers = new NetworkList<NetworkObjectReference>(
        null, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    //가끔 바뀌는 정보?
    [SerializeField] int spawnInterval = 0;
    float triggerTimeOut = 10f;

    //네트워크 스폰시 변수들 초기화
    public override void OnNetworkSpawn()
    {
        minNode = data.minNode;
        maxNode = data.maxNode;
        lifeTime = data.lifeTime;
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        //밟음 판정 서버에서만
        //if (!IsServer) return;

        //캐릭터가 밟으면 목록에서 찾아보고, 있으면 저장 없으면 패스
        if (other.TryGetComponent(out enteredPlayer))
        {
            if (!enteredPlayer.IsSpawned)
            {
                //Debug.Log("스폰 안된놈임");
                return;
            }
            isCheckingTrigger = true;

            //플레이어 매니저가 각 클라이언트마다 따로라서 밟은 캐릭터의 오너가 실행
            if (NetworkManager.Singleton.LocalClientId == enteredPlayer.OwnerClientId)
            {
                FindCurrentCharacters();
            }
            isCheckingTrigger = false;
            //FindCurrentCharactersRpc(enteredPlayer, RpcTarget.Single(enteredPlayer.OwnerClientId, RpcTargetUse.Temp));
        }
    }

    void FindCurrentCharacters()
    {
        foreach (var character in PlayerManager.Instance.currentCharacters)
        {
            //Debug.Log("목록에 있는 id : " + character.GetComponent<NetworkObject>().NetworkObjectId + " 트리거 들어온놈 id + " + enteredCharacter.NetworkObjectId);

            if (character.GetComponent<NetworkObject>().NetworkObjectId == enteredPlayer.NetworkObjectId)
            {
                //찾음
                //Debug.Log("목록에서 찾음");
                AddPlayerRpc(enteredPlayer);
                return;
            }
        }

        //소환된 플레이어 캐릭터 목록에 없으면 리턴
        //Debug.Log("목록에 없는 캐릭터입니다");
        isCheckingTrigger = false;
        return;
    }

    protected virtual void OnTriggerExit(Collider other)
    {
        //나감 판정 서버에서만
        //if (!IsServer) return;

        //나가는놈이 트리거 들어와있는거 리스트에 없으면 탈출
        if (other.TryGetComponent(out exitPlayer))
        {
            //주인만 실행
            if (NetworkManager.Singleton.LocalClientId == exitPlayer.OwnerClientId)
            {
                RemovePlayerRpc(exitPlayer);
            }
        }
    }

    //리스트는 서버에서만 관리
    [Rpc(SendTo.Server)]
    void AddPlayerRpc(NetworkObjectReference player)
    {
        NetworkObject playerObject;
        if(!player.TryGet(out playerObject))
        {
            Debug.Log("AddPlayerRpc : 네트워크 오브젝트 찾을 수 없음");
            return;
        }
        enteredPlayers.Add(player);
        //Debug.Log(playerObject.NetworkObjectId + " 들어옴, " + "리스트 수 : " + enteredPlayers.Count);

        isCheckingTrigger = false;
    }

    [Rpc(SendTo.Server)]
    void RemovePlayerRpc(NetworkObjectReference player)
    {
        NetworkObject playerObject;
        if (!player.TryGet(out playerObject))
        {
            Debug.Log("RemovePlayerRpc : 네트워크 오브젝트 찾을 수 없음");
            return;
        }
        //리스트에 없으면 리턴
        if (!enteredPlayers.Contains(player))
        {
            Debug.Log("RemovePlayerRpc : 리스트에 없음");
            return;
        }

        //있으면 리스트에서 삭제
        //Debug.Log(playerObject.NetworkObjectId + "나감");
        enteredPlayers.Remove(player);
    }

    [Rpc(SendTo.Server)]
    public virtual void TurnIncreaseRpc()
    {
        turnAfterSpawned.Value++;

        //제한 턴이 되면 노드 삭제
        if(lifeTime <= turnAfterSpawned.Value)
        {
            //99보다 크면 무한히 지속되도록 함
            if (lifeTime > 99) return;

            Debug.Log("지속턴 다 됨");
            DeactiveNodeRpc();
        }
    }

    //발판 밟은 플레이어 이벤트 발생시킴
    //이벤트 시작할지 안할지 결정
    [Rpc(SendTo.Server)]
    public virtual void EventStartRpc()
    {
        //아무도 안밟고 있으면 일 없음
        if(enteredPlayers.Count == 0)
        {
            Debug.Log("아무도 안밟음");
            return;
        }
    }

    IEnumerator WaitForTrigger()
    {
        float time = Time.time;

        while (Time.time - time < triggerTimeOut)
        {
            //1초마다 트리거 체크 변수 체크
            yield return new WaitForSecondsRealtime(1);

            //체크 됐으면 탈출
            if (!isCheckingTrigger)
            {
                break;
            }
        }
    }

    //이벤트 시작할때 처리
    [Rpc(SendTo.Server)]
    protected void EventExcuteRpc()
    {
        if (!isEventRunning.Value)
        {
            isEventRunning.Value = true;
        }
        Debug.Log(gameObject.name + " 이벤트 처리 시작");
    }

    //이벤트 끝날때 처리
    [Rpc(SendTo.Server)]
    protected void EventEndRpc()
    {
        if (isEventRunning.Value)
        {
            isEventRunning.Value = false;
        }
        Debug.Log(gameObject.name + " 이벤트 처리 끝");
    }

    [Rpc(SendTo.Everyone)]
    protected virtual void ActiveNodeRpc(Vector3 pos)
    {
        //모든 변수 초기화
        turnAfterSpawned.Value = 0;

        //위치 옮기고
        gameObject.transform.position = pos;

        //스폰
        gameObject.SetActive(true);
    }

    [Rpc(SendTo.Server)]
    protected virtual void DeactiveNodeRpc()
    {
        //OnDeactive?.Invoke(spawnInterval);
        ////이벤트 없앰
        //gameObject.SetActive(false);
        Debug.Log("비활성화 할 노드 전달");
        EventNodeManager.Instance.ScheduleDespawn(this);
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void AssignNodeRpc(NetworkObjectReference node)
    {
        if (node.TryGet(out NetworkObject nodeNetObj))
        {
            Debug.Log("네트워크 오브젝트 못찾음");
        }

        this.node = nodeNetObj.GetComponent<Node>();
    }
}
