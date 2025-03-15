using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class Item : MonoBehaviour
{
    private Button button;
    private bool isToggled = false;
    private ItemName itemName;
    private Coroutine coroutine;
    [SerializeField] TextMeshProUGUI itemTmp;
    void Start()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(ToggleState);
        switch (itemName)
        {
            case ItemName.ChanceUp:
                itemTmp.text = "+1";
                break;
            case ItemName.ReverseMove:
                itemTmp.text = "<>";
                break;
            case ItemName.Obstacle:
                itemTmp.text = "ㅁ";
                break;
        }
    }

    void ToggleState()
    {
        Item currentItem = ItemManager.Instance.ReturnCurrentItem();
        if (currentItem != null && currentItem!=this)
        {
            currentItem.isToggled = !isToggled;
            currentItem.button.GetComponent<Image>().color = Color.white;
        }
        isToggled = !isToggled;
        button.GetComponent<Image>().color = isToggled ? Color.gray : Color.white;
        if (isToggled)
        {
            Debug.Log("Item selected");
            ItemManager.Instance.currentItem = gameObject;
            if (itemName == ItemName.ChanceUp)
            {
                YutManager.Instance.throwChance++;
                ItemManager.Instance.RemoveItem();
            }
            if (itemName == ItemName.ReverseMove)
            {
                GameManager.Instance.announceCanvas.ShowAnnounceText("Choose target!",2f);
                coroutine = StartCoroutine(ChooseTarget());
            }
            if(itemName == ItemName.Obstacle)
            {
                GameManager.Instance.announceCanvas.ShowAnnounceText("Choose Node!", 2f);
                coroutine = StartCoroutine(ChooseNode());
            }
        }
        else
        {
            Debug.Log("Item deselected");
            StopCoroutine(coroutine);
            ItemManager.Instance.currentItem = null;
        }
    }
    /*아이템 적용 대상 선택(상대)*/
    //디버프 아이템을 적용할 상대 말 캐릭터 선택
    private IEnumerator ChooseTarget()
    {
        while (true)
        {
            if (Input.GetMouseButtonDown(0)) // 마우스 클릭 감지
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    if (hit.collider.TryGetComponent<NetworkObject>(out NetworkObject no))
                    {
                        if (no.OwnerClientId != NetworkManager.Singleton.LocalClientId)
                        {
                            Debug.Log("Find target");
                            ItemManager.Instance.RemoveItem();
                            ItemManager.Instance.SpawnItemEffectServerRpc(no);
                            ItemManager.Instance.SetItemServerRpc(no, true);
                            break;
                        }
                    }
                }
            }
            yield return null;
        }
    }
    private IEnumerator ChooseNode()
    {
        while (true)
        {
            if (Input.GetMouseButtonDown(0)) // 마우스 클릭 감지
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    if (hit.collider.TryGetComponent<Node>(out Node no))
                    {
                        Debug.Log("Find Node");
                        ItemManager.Instance.SetObstacleServerRpc(no.transform.position);
                        ItemManager.Instance.RemoveItem();
                    }
                }
            }
            yield return null;
        }
    }
    public bool IsToggled()
    {
        return isToggled;   
    }
    public void SetItemName(ItemName name)
    {
        itemName = name;
    }
    public ItemName GetItemName()
    {
        return itemName;
    }
}
