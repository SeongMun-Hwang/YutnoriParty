using Unity.Netcode;
using UnityEngine;

public class EventNode : NetworkBehaviour
{
    [SerializeField] EventNodeData data;
    //참조만 할 정보들 => 스크립터블 오브젝트에 저장 가능
    int minNode = 1;
    int maxNode = 1;
    protected int lifeTime = 0;
    public int MinNode { get { return minNode; } }
    public int MaxNode { get {return maxNode; } }
    

    //실시간으로 바뀌어야 할 정보들
    int turnAfterSpawned = 0;
    Collider enteredPlayer;

    //네트워크 스폰시 변수들 초기화
    public override void OnNetworkSpawn()
    {
        minNode = data.minNode;
        maxNode = data.maxNode;
        lifeTime = data.lifeTime;
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        //플레이어가 밟으면 걔 저장
        if(other.tag == "Player")
        {
            enteredPlayer = other;
        }
    }
    protected virtual void OnTriggerExit(Collider other)
    {
        if (other.tag == "Player")
        {
            enteredPlayer = null;
        }
    }

    public virtual void TurnIncrease()
    {
        turnAfterSpawned++;

        //제한 턴이 되면 노드 삭제
        if(lifeTime <= turnAfterSpawned)
        {
            DeActiveNode();
        }
    }

    public virtual void EventStart()
    {
        //아무도 안밟고 있으면 일 없음
        if(enteredPlayer == null) return;


    }

    protected virtual void ActiveNode(Transform node)
    {
        //모든 변수 초기화
        turnAfterSpawned = 0;

        //위치 옮기고
        gameObject.transform.position = node.position;

        //스폰
        gameObject.SetActive(true);
    }

    protected virtual void DeActiveNode()
    {
        //이벤트 없앰
        gameObject.SetActive(false);
    }
}
