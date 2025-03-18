using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.UI;

public enum ItemName
{
    ChanceUp,
    ReverseMove,
    Obstacle,
    None
}
public class ItemManager : NetworkBehaviour
{
    static private ItemManager instance;
    static public ItemManager Instance
    {
        get { return instance; }
    }
    private List<GameObject> itemLists = new List<GameObject>();
    [SerializeField] Transform spawnTransform;
    [SerializeField] GameObject itemPrefab;
    [SerializeField] GameObject obstaclePrefab;
    [SerializeField] GameObject reverseEffectPrefab;
    [SerializeField] Sprite obstacleImg;
    [SerializeField] Sprite reverseImg;
    [SerializeField] Sprite chanceUpImg;

    public GameObject currentItem;
    public override void OnNetworkSpawn()
    {
        if (IsClient) { instance = this; }
    }
    private void Update()
    {
        if(Input.GetKeyUp(KeyCode.Keypad0))
        {
            GetItemClientRpc(0);
        }
        if (Input.GetKeyUp(KeyCode.Keypad1))
        {
            GetItemClientRpc(1);
        }
    }
    [ClientRpc]
    public void GetItemClientRpc(ulong targetId)
    {
        Debug.Log("Player" + targetId + "Get Item");
        if (NetworkManager.Singleton.LocalClientId != targetId) return;
        GameObject go = Instantiate(itemPrefab, transform.position, Quaternion.identity, spawnTransform);
        switch (RandomItem())
        {
            case ItemName.ChanceUp:            
                go.GetComponent<Item>().SetItemName(ItemName.ChanceUp);
                go.GetComponent<Item>().itemImg.sprite = chanceUpImg;
                break;
            case ItemName.ReverseMove:
                go.GetComponent<Item>().SetItemName(ItemName.ReverseMove);
                go.GetComponent<Item>().itemImg.sprite = reverseImg;
                break;
            case ItemName.Obstacle:
                go.GetComponent<Item>().SetItemName(ItemName.Obstacle);
                go.GetComponent<Item>().itemImg.sprite = obstacleImg;
                break;
        }
        itemLists.Add(go);

    }
    private ItemName RandomItem()
    {
        Array itemEnum=Enum.GetValues(typeof(ItemName));
        ItemName name = (ItemName)itemEnum.GetValue(UnityEngine.Random.Range(0, itemEnum.Length));
        if (name == ItemName.None) return RandomItem();
        return name;
    }
    public ItemName CheckItemActive()
    {
        Debug.Log("Check actived item");
        foreach (var item in itemLists)
        {
            if (item.GetComponent<Item>().IsToggled())
            {
                return item.GetComponent<Item>().GetItemName();
            }
        }
        return ItemName.None;
    }
    public void RemoveItem()
    {
        if (currentItem != null)
        {
            Debug.Log("Remove item");
            itemLists.Remove(currentItem);
            Destroy(currentItem);
            currentItem = null;
        }
    }
    public Item ReturnCurrentItem()
    {
        if(currentItem != null) return currentItem.GetComponent<Item>();
        return null;    
    }
    [ServerRpc(RequireOwnership = false)]
    public void SetItemServerRpc(NetworkObjectReference noRef, bool value)
    {
        noRef.TryGet(out NetworkObject no);
        no.GetComponent<CharacterInfo>().isReverse.Value = value;
    }
    [ServerRpc(RequireOwnership =false)]
    public void SetObstacleServerRpc(Vector3 pos)
    {
        GameObject go=Instantiate(obstaclePrefab,pos,Quaternion.identity);
        go.GetComponent<NetworkObject>().Spawn();
    }
    [ServerRpc(RequireOwnership = false)]
    public void SpawnItemEffectServerRpc(NetworkObjectReference noRef)
    {
        noRef.TryGet(out NetworkObject no);
        int n = no.GetComponent<CharacterInfo>().overlappedCount;
        GameObject go = Instantiate(reverseEffectPrefab, no.transform.position 
            + new Vector3(0, 2*(n+1), 0), Quaternion.identity);
        go.GetComponent<NetworkObject>().Spawn();
        go.GetComponent<NetworkObject>().TrySetParent(no.transform);
        no.GetComponent<CharacterInfo>().ItemEffect = go;
    }
    [ServerRpc(RequireOwnership =false)]
    public void DespawnItemEffectServerRpc(NetworkObjectReference noRef)
    {
        noRef.TryGet(out NetworkObject no);
        GameObject go = no.GetComponent<CharacterInfo>().ItemEffect;
        if(go != null)
        {
            go.GetComponent<NetworkObject>().Despawn();
            Destroy(go);
        }
    }
    [ServerRpc(RequireOwnership = false)]
    public void ItemUseAnnounceServerRpc(string text, ulong clientId)
    {
        string playerName = PlayerManager.Instance.RetrunPlayerName(clientId);
        GameManager.Instance.announceCanvas.ShowAnnounceTextClientRpc(playerName + "가 " + text + " 아이템을 사용했습니다!", 2f);
    }
}
