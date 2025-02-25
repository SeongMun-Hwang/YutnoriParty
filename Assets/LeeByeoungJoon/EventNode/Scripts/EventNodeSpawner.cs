using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

enum EventNodeType
{
    Island,
    BlackHole,
    Item
} 

//얘는 이벤트 노드 스폰, 턴 진행만 관리
public class EventNodeSpawner : NetworkBehaviour
{
    //각 이벤트 노드 종류별로 리스트를 들고 있음??
    //그냥 이벤트 노드로 들고 있고 각 인스턴스에 맡김??
    [SerializeField] List<EventNode> spawnedNodes;

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
    
    void FixedUpdate()
    {
        //임시로 턴 돌아가는 카운터 이걸로 씀
        if (Input.GetKeyDown(KeyCode.Z))
        {
            TurnCount();
        }
    }

    public override void OnNetworkSpawn()
    {
        //서버에서만 노드 관리
        if (!IsServer) return;

        //방 비었는지 체크하는 리스트 초기화
        isFulledNode = new List<int>();

        for(int i=0; i<blackHoleNodePrefab.MaxNode; i++)
        {
            SpawnEventNode(GetRandomPosition(blackHoleNodeTransforms), EventNodeType.BlackHole);
        }
    }

    //모든 플레이어 한바퀴 돌면 이거 실행
    public void TurnCount()
    {
        foreach (var node in spawnedNodes)
        {
            node.TurnIncrease();
        }
    }

    //EndMove에서 얘 실행
    public void CheckStepOn()
    {
        //모든 노드에서 이벤트 실행
        foreach(var node in spawnedNodes)
        {
            node.EventStart();
        }
    }

    //노드 리스트를 받아 그 중 랜덤한 노드 위치를 골라주는 함수
    Transform GetRandomPosition(List<Transform> transforms)
    {
        List<Transform> tmp = transforms;

        //랜덤으로 노드 하나 뽑고
        Transform nodeTransform = tmp[Random.Range(0, tmp.Count)];
        //전체 노드에서 얘 인덱스가 몇번인지 찾고
        int idx = allNodes.IndexOf(nodeTransform);
        Debug.Log("인덱스 : " + idx);

        //방 다찼으면 다시 찾으셈
        while (isFulledNode.Contains(idx))
        {
            Debug.Log("방 찼네요");
            //방 다찼으면 tmp에서 제거해 다시 안뽑히게 하고 
            tmp.Remove(nodeTransform);
            //다시 뽑고
            nodeTransform = tmp[Random.Range(0, tmp.Count)];
            //인덱스 다시 뽑아서 검사
            idx = allNodes.IndexOf(nodeTransform);
            Debug.Log("다시 뽑은 인덱스 : " + idx);
        }

        //최종적으로 트랜스폼으로 리턴

        return allNodes[idx];
    }

    void SpawnEventNode(Transform nodeTransform, EventNodeType type)
    {
        //해당 타입 노드를 트랜스폼에 인스턴시에이트
        switch (type)
        {
            case EventNodeType.Island:
                spawnedNodes.Add(Instantiate(islandNodePrefab, nodeTransform.position, Quaternion.identity));
                break;
            case EventNodeType.BlackHole:
                spawnedNodes.Add(Instantiate(blackHoleNodePrefab, nodeTransform.position, Quaternion.identity));
                break;
            case EventNodeType.Item:
                spawnedNodes.Add(Instantiate(itemNodePrefab, nodeTransform.position, Quaternion.identity));
                break;
            default:
                Debug.Log("에러 : 이벤트 타입 판별 불가");
                break;
        }

        //네트워크 스폰
        spawnedNodes.Last().GetComponent<NetworkObject>().Spawn();
    }
}
