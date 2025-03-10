using UnityEngine;
using UnityEngine.UI;

public class Item : MonoBehaviour
{
    private Button button;
    private bool isToggled = false;
    private ItemName ItemName;
    void Start()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(ToggleState);
    }

    void ToggleState()
    {
        isToggled = !isToggled;
        button.GetComponent<Image>().color = isToggled ? Color.gray : Color.white;
        if (isToggled)
        {
            Debug.Log("Item selected");
            ItemManager.Instance.currentItem = gameObject;
        }
        else
        {
            Debug.Log("Item deselected");
            ItemManager.Instance.currentItem = null;
        }
    }
    public bool IsToggled()
    {
        return isToggled;   
    }
    public void SetItemName(ItemName name)
    {
        ItemName = name;
    }
    public ItemName GetItemName()
    {
        return ItemName;
    }
}
