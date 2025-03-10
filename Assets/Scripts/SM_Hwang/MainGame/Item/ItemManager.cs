using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

public enum ItemName
{
    ResultUp,
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
    public GameObject currentItem;
    public override void OnNetworkSpawn()
    {
        if (IsClient) { instance = this; }
    }

    [ClientRpc]
    public void GetItemClientRpc(ItemName item, ulong targetId)
    {
        Debug.Log("Player" + targetId + "Get Item");
        if (NetworkManager.Singleton.LocalClientId != targetId) return;
        switch (item)
        {
            case ItemName.ResultUp:
                GameObject go = Instantiate(itemPrefab, transform.position, Quaternion.identity, spawnTransform);
                go.GetComponent<Item>().SetItemName(ItemName.ResultUp);
                itemLists.Add(go);
                break;
        }
    }
    public ItemName CheckItemActive()
    {
        Debug.Log("Check actived item");
        foreach(var item in itemLists)
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
}
