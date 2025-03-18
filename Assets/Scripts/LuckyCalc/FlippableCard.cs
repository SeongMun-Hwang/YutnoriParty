using TMPro;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class FlippableCard : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private GameObject BackImage;
    [SerializeField] private TMP_Text FrontText;

    public LuckyCalcManager manager;
    private Color32 defaultColor;
    private Image image;

    public int Id;
    public int Number;
    public bool isFlipped;

    public void Start()
    {
        image = GetComponent<Image>();
        defaultColor = image.color;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isFlipped)
        {
            isFlipped = true;
            manager.TryToFlip(Id);
        }
    }

    public void OpenCard()
    {
        Debug.Log($"{Number} 카드 열기");
        BackImage.SetActive(false);
        FrontText.text = Number.ToString();
        image.color = Color.gray;
        FrontText.gameObject.SetActive(true);
    }

    public void CloseCard()
    {
        Debug.Log($"{Number} 카드 닫기");
        FrontText.gameObject.SetActive(false);
        BackImage.SetActive(true);
        image.color = defaultColor;
        isFlipped = false;
    }
}