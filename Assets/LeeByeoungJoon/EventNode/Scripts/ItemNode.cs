using Unity.Netcode;
using UnityEngine;

public class ItemNode : EventNode
{
    [SerializeField] GameObject getEffect;
    [SerializeField] Transform effectPos;

    [Rpc(SendTo.Server)]
    public override void EventStartRpc()
    {
        EventExcuteRpc();
        //아무도 안밟고 있으면 일 없음
        if (enteredPlayers.Count == 0)
        {
            Debug.Log("아이템 노드 아무도 안밟음");
            EventEndRpc();
            return;
        }

        if (!enteredPlayers[enteredPlayers.Count - 1].TryGet(out NetworkObject character))
        {
            Debug.Log("네트워크 오브젝트 없음");
            EventEndRpc();
            return;
        }

        Debug.Log(character.OwnerClientId + "플레이어 아이템 획득");

        
        GameObject go = Instantiate(getEffect, effectPos); //파티클 재생
        go.GetComponent<NetworkObject>().Spawn();
        PlayItemSoundRpc(16);

        ItemManager.Instance.GetItemClientRpc(character.OwnerClientId);
        DeactiveNodeRpc();
        EventEndRpc();
    }

    [Rpc(SendTo.ClientsAndHost)]
    void PlayItemSoundRpc(int idx)
    {
        AudioManager.instance.Playsfx(idx);
    }
}