using TMPro;
using UnityEngine;

public class YutResults : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI yutText;
    
    public void SetYutText(string text)
    {
        yutText.text = text;
    }
}
