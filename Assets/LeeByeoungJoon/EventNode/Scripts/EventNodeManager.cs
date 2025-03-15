using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public enum EventNodeType
{
    Island,
    BlackHole,
    Item
} 

//얘는 이벤트 노드 스폰, 턴 진행만 관리
public class EventNodeManager : NetworkBehaviour
{
    //각 이벤트 노드 종류별로 리스트를 들고 있음??
    //그냥 이벤트 노드로 들고 있고 각 인스턴스에 맡김??
    List<EventNode> spawnedNodes;

    //인스턴시에이트 해야하니까 노드는 다 갖고 있어야함
    [SerializeField] EventNode islandNodePrefab;
    [SerializeField] EventNode blackHoleNodePrefab;
    [SerializeField] EventNode itemNodePrefab;

    //모든 노드들
    [SerializeField] List<Node> allNodes;

    [SerializeField] List<Node> islandNodes;
    [SerializeField] List<Node> blackHoleNodes;
    [SerializeField] List<Node> blackHoleTargets;
    [SerializeField] List<Node> itemNodes;

    //노드에 이벤트 노드가 없는지 체크하는 리스트, 여기에 있으면 방 나간거
    List<int> isFulledNode;
    List<EventNode> despawnSchedule; //디스폰 예약용 리스트
    //각 이벤트 노드별로 몇개 있는지 세는 딕셔너리
    Dictionary<EventNodeType, int> eventNodeTypeNum;

    int itemNodeInitialSpawnNum = 2;

    //싱글톤 아님
    static EventNodeManager instance;
    public static EventNodeManager Instance {  get { return instance; } }

    public NetworkVariable<bool> checkingStepOn = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone);

    private void Awake()
    {
        instance = this;
    }

    void OnCheckingStepOnChanged(bool prev, bool current)
    {
        Debug.Log("체킹 스텝온 바뀜 : " + checkingStepOn.Value + " 이전 값 :" + prev + " 현재 값 : " + current);
    }

    void FixedUpdate()
    {
        //if (!IsServer) return;
        //임시로 턴 돌아가는 카운터 이걸로 씀
        //if (Input.GetKeyDown(KeyCode.Z))
        //{
        //    TurnCountServerRpc();
        //}
        //if(Input.GetKeyDown(KeyCode.X))
        //{
        //    CheckStepOnServerRpc();
        //}
    }

    public override void OnNetworkSpawn()
    {
        //checkingStepOn.OnValueChanged += OnCheckingStepOnChanged;

        //서버에서만 노드 관리
        if (!IsServer) return;

        //리스트 등 초기화
        //방 비었는지 체크하는 리스트 초기화
        isFulledNode = new List<int>();
        despawnSchedule = new List<EventNode>();
        spawnedNodes = new List<EventNode>();

        //이벤트 노드 체크하는 딕셔너리 초기화
        eventNodeTypeNum = new Dictionary<EventNodeType, int>();
        foreach(EventNodeType type in Enum.GetValues(typeof(EventNodeType)))
        {
            eventNodeTypeNum[type] = 0;
        }

        //이벤트 노드 생성 -> 이벤트 노드 더 많이 만들거면 여기 리팩토링 해야함
        SpawnEventNode(GetRandomNode(blackHoleNodes), EventNodeType.BlackHole);
        SpawnEventNode(GetRandomNode(islandNodes), EventNodeType.Island);
        SpawnItemNodeRpc(itemNodeInitialSpawnNum);
    }

    //모든 플레이어 한바퀴 돌면 이거 실행
    [ServerRpc]
    public void TurnCountServerRpc()
    {
        //서버에서만 실행
        if(!IsServer) return;

        Debug.Log("한바퀴 증가");

        foreach (var node in spawnedNodes)
        {
            node.TurnIncreaseRpc();
        }

        //한바퀴에 아이템노드 하나씩 생성
        SpawnItemNodeRpc(1);
    } 

    //EndMove에서 얘 실행
    [ServerRpc(RequireOwnership = false)]
    public void CheckStepOnServerRpc()
    {
        //서버에서만 실행
        if (!IsServer) return;

        checkingStepOn.Value = true; //노드 검사 시작

        Debug.Log("노드 검사 시작");

        //모든 노드에서 이벤트 실행
        foreach (var node in spawnedNodes)
        {
            node.EventStartRpc();
        }

        StartCoroutine(WaitForEventExcute());
    }

    IEnumerator WaitForEventExcute()
    {
        int timeOut = 10;
        bool eventRunning = true;

        while (eventRunning)
        {
            //모든 노드들의 이벤트 실행중 여부를 검사
            foreach (var node in spawnedNodes)
            {
                //하나라도 실행중이면 eventRunning은 true
                if (node.isEventRunning.Value)
                {
                    eventRunning = true;
                    break;
                }

                //모두 통과하면 다 실행 종료인걸로 보고 eventRunning은 false
                eventRunning = false;
            }

            if (!eventRunning)
            {
                Debug.Log("이벤트 실행 완료");
                break;
            }

            yield return new WaitForSeconds(1);
            //timeOut--;
            Debug.Log("이벤트 기다리는 중..");
            //if(timeOut == 0)
            //{
            //    Debug.Log("이벤트 대기 타임아웃");
            //    break;
            //}
        }

        if (eventRunning)
        {
            Debug.Log("이벤트 실행중이지만 타임아웃");
            yield return null;
        }

        DespawnEventNodeExcute();

        checkingStepOn.Value = false; //노드 검사 끝
        GameManager.Instance.mainGameProgress.ChangeEventCheckingClientRpc(false);

        Debug.Log("노드 검사 끝");

        yield return null;
    }

    void DespawnEventNodeExcute()
    {
        //예약 걸린 노드들 삭제
        foreach (var node in despawnSchedule)
        {
            DespawnEventNode(node);
        }
        //혹시 모르니까 클리어해줌
        despawnSchedule.Clear();
    }

    //노드 리스트를 받아 그 중 랜덤한 노드 위치를 골라주는 함수
    Node GetRandomNode(List<Node> nodes)
    {
        List<Node> tmp = nodes;

        //랜덤으로 노드 하나 뽑고
        Node node = tmp[UnityEngine.Random.Range(0, tmp.Count)];
        //전체 노드에서 얘 인덱스가 몇번인지 찾고
        int idx = allNodes.IndexOf(node);
        Debug.Log("인덱스 : " + idx);

        //방 찼으면 다시 찾으셈
        while (isFulledNode.Contains(idx))
        {
            //더 이상 빈 방이 없으면 
            if(tmp.Count == 0)
            {
                //null을 리턴
                return null;
            }
            Debug.Log("방 찼네요");
            //방 다찼으면 tmp에서 제거해 다시 안뽑히게 하고 
            tmp.Remove(node);
            //다시 뽑고
            node = tmp[UnityEngine.Random.Range(0, tmp.Count)];
            //인덱스 다시 뽑아서 검사
            idx = allNodes.IndexOf(node);
            Debug.Log("다시 뽑은 인덱스 : " + idx);
        }

        //최종적으로 노드 리턴
        return allNodes[idx];
    }

    void SpawnEventNode(Node node, EventNodeType type)
    {
        if (node == null)
        {
            Debug.Log("빈 노드 없음");
            return;
        }
        Debug.Log("노드 타입 : " +  type);

        EventNode nodePrefab;
        Transform nodeTransform = node.transform;

        switch (type)
        {
            case EventNodeType.Island:
                nodePrefab = islandNodePrefab;
                break;
            case EventNodeType.BlackHole:
                nodePrefab = blackHoleNodePrefab;
                break;
            case EventNodeType.Item:
                nodePrefab = itemNodePrefab;
                break;
            default:
                Debug.Log("에러 : 이벤트 타입 판별 불가");
                return;
        }

        //Debug.Log(nodePrefab + " 할당");

        //해당 타입 노드가 최대 노드 수 까지 만들어졌다면 더 안만들고 리턴
        if (nodePrefab.MaxNode <= eventNodeTypeNum[type])
        {
            Debug.Log(type + " 노드 타입 최대");
            return;
        }

        //해당 타입 노드를 트랜스폼에 인스턴시에이트하고 리스트에 추가
        spawnedNodes.Add(Instantiate(nodePrefab, nodeTransform.position, Quaternion.identity));
        //Debug.Log(type + " 인스턴스화 성공");

        var spawnedNode = spawnedNodes.Last();

        //노드 할당
        spawnedNode.AssignNodeRpc(node.GetComponent<GameObject>());

        //블랙홀 노드면 범위 노드 전달
        if(type == EventNodeType.BlackHole)
        {
            spawnedNode.GetComponent<BlackHoleNode>().nodes = blackHoleTargets;
        }

        //네트워크 스폰
        spawnedNode.GetComponent<NetworkObject>().Spawn();
        //Debug.Log(type + " 네트워크 스폰 성공");

        //모든 처리가 끝나 노드가 생성되었으면 딕셔너리 숫자 증가
        eventNodeTypeNum[type]++;
        //Debug.Log(type + " 딕셔너리++");

        //방 찼다는거 표시
        isFulledNode.Add(allNodes.IndexOf(node));

        //Debug.Log(type + " 스폰 완료");
    }

    void DespawnEventNode(EventNode node)
    {
        //리스트에서 빼고
        spawnedNodes.Remove(node);

        //딕셔너리 숫자 감소
        eventNodeTypeNum[node.data.eventNodeType]--;
        Debug.Log(node.name + " 비활성화 함");

        //해당 노드를 파괴(디스폰)
        node.GetComponent<NetworkObject>().Despawn();
        Destroy(node.gameObject);
    }

    //디스폰 예약 리스트에 추가
    public void ScheduleDespawn(EventNode node)
    {
        despawnSchedule.Add(node);
    }

    [Rpc(SendTo.Server)]
    public void EscapeIslandCallRpc(NetworkObjectReference no)
    {
        if (!no.TryGet(out NetworkObject character))
        {
            Debug.Log("네트워크 오브젝트 없음");
            return;
        }

        //스폰 노드중에 무인도 노드 찾아서 캐릭터 삭제 시도요청
        foreach(var node in spawnedNodes)
        {
            IslandNode island;
            if (node.TryGetComponent(out island))
            {
                island.EscapeIslandRpc(character);
            }
        }
    }

    [Rpc(SendTo.Server)]
    public void SpawnItemNodeRpc(int spawnNum)
    {
        int spawned = eventNodeTypeNum[EventNodeType.Item];
        int max = itemNodePrefab.GetComponent<ItemNode>().MaxNode;
        
        //스폰할 노드 수가 최대 생성개수를 넘어서지 못하게 제한
        int toSpawn = (spawnNum + spawned > max) ? max - spawned : spawnNum;

        //스폰할 노드 수만큼 스폰
        for(int i = 0; i < toSpawn; i++)
        {
            SpawnEventNode(GetRandomNode(itemNodes), EventNodeType.Item);
        }
    }
}
