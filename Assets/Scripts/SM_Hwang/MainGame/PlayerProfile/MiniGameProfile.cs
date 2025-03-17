using TMPro;
using UnityEngine;

public class MiniGameProfile : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI userNameTmp;
    [SerializeField] private TextMeshProUGUI statusTmp;
    public void SetName(string name)
    {
        userNameTmp.text = name;
    }
    public void SetStatus(string status)
    {
        statusTmp.text = status;
        if (status == "Dead")
        {
            statusTmp.color = Color.red;
        }
        else if (status == "Live")
        {
            statusTmp.color = Color.green;
        }
        else if(status == "Spectator")
        {
            statusTmp.color = Color.blue;
        }
    }
}
