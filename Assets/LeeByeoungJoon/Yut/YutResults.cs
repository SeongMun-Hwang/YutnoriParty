using TMPro;
using Unity.Netcode;
using UnityEngine;

public class YutResults : NetworkBehaviour
{
    [SerializeField] TextMeshProUGUI yutText;
    YutResult yutResult;
    
    public void SetYutText(YutResult result)
    {
        yutResult = result;
        yutText.text = result.ToString();
    }

    public void OnButtonPressed()
    {
        //�� ĭ �����ϴ��� ���� ��ȯ
        switch (yutResult)
        {
            case YutResult.BackDo:
                Debug.Log("-1");
                break;
            case YutResult.Do:
                Debug.Log("1");
                break;
            case YutResult.Gae:
                Debug.Log("2");
                break;
            case YutResult.Gur:
                Debug.Log("3");
                break;
            case YutResult.Yut:
                Debug.Log("4");
                break;
            case YutResult.Mo:
                Debug.Log("5");
                break;
        }

        //��ư ����
        Destroy(gameObject);
    }
}
