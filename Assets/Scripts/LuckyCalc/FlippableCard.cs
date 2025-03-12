using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class FlippableCard : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] TMP_Text numberText;
    public NumberPanel numberPanel;
    public int number;

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log(number);
        numberText.gameObject.SetActive(true);
        numberText.text = number.ToString();
        numberPanel.ChangeOpernad(number);
    }
}
