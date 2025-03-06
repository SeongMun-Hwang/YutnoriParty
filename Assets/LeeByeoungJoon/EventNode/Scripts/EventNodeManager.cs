using System;
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
    [SerializeField] List<Transform> allNodes;
    //스폰될 위치들(스크립터블 오브젝트로 바꾸려면 vec3와 transform 간 변환 과정이 필요)
    [SerializeField] List<Transform> islandNodeTransforms;
    [SerializeField] List<Transform> blackHoleNodeTransforms;
    [SerializeField] List<Transform> itemNodeTransforms;

    //노드에 이벤트 노드가 없는지 체크하는 리스트, 여기에 있으면 방 나간거
    List<int> isFulledNode;
    List<EventNode> despawnSchedule; //디스폰 예약용 리스트
    //각 이벤트 노드별로 몇개 있는지 세는 딕셔너리
    Dictionary<EventNodeType, int> eventNodeTypeNum;

    //싱글톤 아님
    static EventNodeManager instance;
    public static EventNodeManager Instance {  get { return instance; } }
    private void Awake()
    {
        instance = this;
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

        //이벤트 노드 생성
        SpawnEventNode(GetRandomPosition(blackHoleNodeTransforms), EventNodeType.BlackHole);
        SpawnEventNode(GetRandomPosition(islandNodeTransforms), EventNodeType.Island);
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
    } 

    //EndMove에서 얘 실행
    [ServerRpc(RequireOwnership = false)]
    public void CheckStepOnServerRpc()
    {
        //서버에서만 실행
        if (!IsServer) return;

        Debug.Log("밟은 노드에 이벤트 있는지 체크");

        //모든 노드에서 이벤트 실행
        foreach (var node in spawnedNodes)
        {
            node.EventStartRpc();
        }

        //예약 걸린 노드들 삭제
        foreach (var node in despawnSchedule)
        {
            DespawnEventNode(node);
        }
        //혹시 모르니까 클리어해줌
        despawnSchedule.Clear();
    }

    //노드 리스트를 받아 그 중 랜덤한 노드 위치를 골라주는 함수
    Transform GetRandomPosition(List<Transform> transforms)
    {
        List<Transform> tmp = transforms;

        //랜덤으로 노드 하나 뽑고
        Transform nodeTransform = tmp[UnityEngine.Random.Range(0, tmp.Count)];
        //전체 노드에서 얘 인덱스가 몇번인지 찾고
        int idx = allNodes.IndexOf(nodeTransform);
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
            tmp.Remove(nodeTransform);
            //다시 뽑고
            nodeTransform = tmp[UnityEngine.Random.Range(0, tmp.Count)];
            //인덱스 다시 뽑아서 검사
            idx = allNodes.IndexOf(nodeTransform);
            Debug.Log("다시 뽑은 인덱스 : " + idx);
        }

        //최종적으로 트랜스폼으로 리턴

        return allNodes[idx];
    }

    void SpawnEventNode(Transform nodeTransform, EventNodeType type)
    {
        if(nodeTransform == null)
        {
            Debug.Log("빈 노드 없음");
            return;
        }
        Debug.Log("노드 타입 : " +  type);

        EventNode nodePrefab;

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
            return;
        }

        //해당 타입 노드를 트랜스폼에 인스턴시에이트하고 리스트에 추가
        spawnedNodes.Add(Instantiate(nodePrefab, nodeTransform.position, Quaternion.identity));
        //Debug.Log(type + " 인스턴스화 성공");

        //네트워크 스폰
        spawnedNodes.Last().GetComponent<NetworkObject>().Spawn();
        //Debug.Log(type + " 네트워크 스폰 성공");

        //모든 처리가 끝나 노드가 생성되었으면 딕셔너리 숫자 증가
        eventNodeTypeNum[type]++;
        //Debug.Log(type + " 딕셔너리++");

        //방 찼다는거 표시
        isFulledNode.Add(allNodes.IndexOf(nodeTransform));

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
}
