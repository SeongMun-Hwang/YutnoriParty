using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class EventNode : NetworkBehaviour
{
    public EventNodeData data;
    //참조만 할 정보들 => 스크립터블 오브젝트에 저장 가능
    int minNode = 1;
    int maxNode = 1;
    protected int lifeTime = 0;
    public int MinNode { get { return minNode; } }
    public int MaxNode { get {return maxNode; } }

    //실시간으로 바뀌어야 할 정보들
    protected NetworkVariable<int> turnAfterSpawned = new NetworkVariable<int>(0);
    protected NetworkObject enteredPlayer;
    protected NetworkObject exitPlayer;
    protected List<NetworkObject> enteredPlayers = new List<NetworkObject>();

    //가끔 바뀌는 정보?
    [SerializeField] int spawnInterval = 0;

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
        if (other.TryGetComponent(out enteredPlayer))
        {
            bool contains = false;
            foreach (var character in PlayerManager.Instance.currentCharacters)
            {
                //Debug.Log("목록에 있는 id : " + character.GetComponent<NetworkObject>().NetworkObjectId + " 트리거 들어온놈 id + " + enteredPlayer.NetworkObjectId);

                if (character.GetComponent<NetworkObject>().NetworkObjectId == enteredPlayer.NetworkObjectId)
                {
                    contains = true;
                }
            }
            Debug.Log("들어있음? : " + contains);

            //소환된 플레이어 캐릭터 목록에 없으면 리턴
            if (!contains) return;

            AddPlayerRpc(enteredPlayer);
        }
    }
    protected virtual void OnTriggerExit(Collider other)
    {
        //나가는놈이 트리거 들어와있는거 리스트에 없으면 탈출
        if (other.TryGetComponent(out exitPlayer))
        {
            RemovePlayerRpc(exitPlayer);
        }
    }

    //리스트는 서버에서만 관리
    [Rpc(SendTo.Server)]
    void AddPlayerRpc(NetworkObjectReference player)
    {
        enteredPlayers.Add(player);
        Debug.Log(player.NetworkObjectId + " 들어옴, " + "리스트 수 : " + enteredPlayers.Count);
    }

    [Rpc(SendTo.Server)]
    void RemovePlayerRpc(NetworkObjectReference player)
    {
        //이미 들어와 있으면 리턴
        if (enteredPlayers.Contains(player)) return;
        if (!enteredPlayers.Contains(player)) return;

        //있으면 리스트에서 삭제
        Debug.Log(player.NetworkObjectId + "나감");
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

    //이벤트를 시작하면 개별적인 처리를 끝내고
    void EventExecute()
    {

    }

    //모든 클라이언트에서 동일하게 효과를 보여줌
    [ClientRpc]
    void PlayEventClientRpc()
    {

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
}
