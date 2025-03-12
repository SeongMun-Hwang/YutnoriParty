using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NumberPanel : MonoBehaviour
{
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private TMP_Text operandText;
    private bool isFlipped = false;

    public void MakeCardDeck(Color32 color)
    {
        List<int> numberList = new List<int>();

        for (int i = 1; i <= 9; i++)
        {
            numberList.Add(i);
        }

        Shuffle(numberList);

        foreach(int i in numberList)
        {
            FlippableCard fc = Instantiate(cardPrefab, transform).GetComponent<FlippableCard>();
            fc.numberPanel = this;
            fc.GetComponent<Image>().color = color;
            fc.number = i;
        }
    }

    private void Shuffle(List<int> list)
    {
        System.Random rand = new System.Random();
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rand.Next(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    public void ChangeOpernad(int n)
    {
        operandText.text = n.ToString();
    }
}
