using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using System;

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
    public GameObject currentItem;
    public override void OnNetworkSpawn()
    {
        if (IsClient) { instance = this; }
    }
    private void Update()
    {
        if(Input.GetKeyUp(KeyCode.Keypad0))
        {
            GetItemClientRpc(ItemName.Obstacle, 0);
        }
        if (Input.GetKeyUp(KeyCode.Keypad1))
        {
            GetItemClientRpc(ItemName.Obstacle, 1);
        }
    }
    [ClientRpc]
    public void GetItemClientRpc(ItemName item, ulong targetId)
    {
        Debug.Log("Player" + targetId + "Get Item");
        if (NetworkManager.Singleton.LocalClientId != targetId) return;
        GameObject go = Instantiate(itemPrefab, transform.position, Quaternion.identity, spawnTransform);
        switch (RandomItem())
        {
            case ItemName.ChanceUp:            
                go.GetComponent<Item>().SetItemName(ItemName.ChanceUp);
                break;
            case ItemName.ReverseMove:
                go.GetComponent<Item>().SetItemName(ItemName.ReverseMove);
                break;
            case ItemName.Obstacle:
                go.GetComponent<Item>().SetItemName(ItemName.Obstacle);
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
}
