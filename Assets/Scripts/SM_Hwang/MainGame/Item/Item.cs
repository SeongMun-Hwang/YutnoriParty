using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Item : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private Button button;
    private bool isToggled = false;
    private ItemName itemName;
    private Coroutine coroutine;
    public Image itemImg;
    void Start()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(ToggleState);
    }

    void ToggleState()
    {
        Item currentItem = ItemManager.Instance.ReturnCurrentItem();
        if (currentItem != null && currentItem != this)
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
                ItemManager.Instance.ItemUseAnnounceServerRpc("한 번 더", NetworkManager.Singleton.LocalClientId);
                ItemManager.Instance.RemoveItem(NetworkManager.Singleton.LocalClientId, itemName);
            }
            if (itemName == ItemName.ReverseMove)
            {
                GameManager.Instance.announceCanvas.ShowAnnounceText("대상을 고르세요!", 2f);
                coroutine = StartCoroutine(ChooseTarget());
            }
            if (itemName == ItemName.Obstacle)
            {
                GameManager.Instance.announceCanvas.ShowAnnounceText("노드를 고르세요!", 2f);
                coroutine = StartCoroutine(ChooseNode());
            }            
            if (itemName == ItemName.Carry)
            {
                GameManager.Instance.announceCanvas.ShowAnnounceText("내 말을 고르세요!", 2f);
                coroutine = StartCoroutine(ChooseCarryCharacter());
            }
        }
        else
        {
            Debug.Log("Item deselected");
            StopCoroutine(coroutine);
            ItemManager.Instance.currentItem = null;
        }
    }
    private IEnumerator ChooseCarryCharacter()
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
                        if (itemName == ItemName.Carry)
                        {
                            if (no.OwnerClientId == NetworkManager.Singleton.LocalClientId)
                            {
                                if (PlayerManager.Instance.currentCharacters.Count==PlayerManager.Instance.ReturnNumOfCharacter())
                                {
                                    GameManager.Instance.announceCanvas.ShowAnnounceText("소환 가능한 말이 없습니다!");
                                    isToggled = false;
                                    ItemManager.Instance.ReturnCurrentItem().GetComponent<Image>().color = Color.white;
                                    yield break;
                                }
                                ItemManager.Instance.CarryCharacter(no.gameObject);
                                ItemManager.Instance.RemoveItem(NetworkManager.Singleton.LocalClientId,itemName);
                                break;
                            }
                        }
                    }
                }
            }
            yield return null;
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
                        if (itemName == ItemName.ReverseMove)
                        {
                            Debug.Log("Find target");
                            if (no.GetComponent<CharacterInfo>().isReverse.Value)
                            {
                                GameManager.Instance.announceCanvas.ShowAnnounceText("이미 혼란 상태 입니다!.", 2f);
                                isToggled = false;
                                ItemManager.Instance.ReturnCurrentItem().GetComponent<Image>().color = Color.white;
                                yield break;
                            }
                            ItemManager.Instance.ItemUseAnnounceServerRpc("혼란", NetworkManager.Singleton.LocalClientId);
                            ItemManager.Instance.RemoveItem(NetworkManager.Singleton.LocalClientId, itemName);
                            ItemManager.Instance.SpawnItemEffectServerRpc(no, NetworkManager.Singleton.LocalClientId);
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
                        ItemManager.Instance.ItemUseAnnounceServerRpc("장애물", NetworkManager.Singleton.LocalClientId);
                        ItemManager.Instance.SetObstacleServerRpc(no.transform.position, NetworkManager.Singleton.LocalClientId);
                        ItemManager.Instance.RemoveItem(NetworkManager.Singleton.LocalClientId, itemName);
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

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (TooltipManager.Instance.IsTooltipActive()) return;
        switch (itemName)
        {
            case ItemName.ChanceUp:
                TooltipManager.Instance.DrawTooltip(
                    "한번 더!", "당신이 윷을 한 번 더 던질 수 있는 기회가 주어집니다.", TooltipManager.TooltipType.Item
                    );
                break;
            case ItemName.ReverseMove:
                TooltipManager.Instance.DrawTooltip(
                    "혼란", "선택한 상대방의 말의 이동방향을 딱 한 번 반대로 설정합니다.", TooltipManager.TooltipType.Item
                    );
                break;
            case ItemName.Obstacle:
                TooltipManager.Instance.DrawTooltip(
                    "장애물", "특정 위치에 장애물을 배치합니다. 장애물에 부딪힌 말은 그 자리에서 이동을 멈춥니다.", TooltipManager.TooltipType.Item
                    );
                break;
            case ItemName.Carry:
                TooltipManager.Instance.DrawTooltip(
                    "업기", "바로 말을 하나 업습니다! 사기템!", TooltipManager.TooltipType.Item
                    );
                break;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        TooltipManager.Instance.EraseTooltip();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        TooltipManager.Instance.EraseTooltip();
        AudioManager.instance.Playsfx(13);
    }

    public void OnDestroy()
    {
        TooltipManager.Instance.EraseTooltip();
    }

    public void OnDisable()
    {
        TooltipManager.Instance.EraseTooltip();
    }
}