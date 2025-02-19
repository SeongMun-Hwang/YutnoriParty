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
                GameManager.Instance.mainGameProgress.currentCharacter.MoveToPrevNode(1);
                //Debug.Log("-1");
                break;
            case YutResult.Do:
                GameManager.Instance.mainGameProgress.currentCharacter.MoveToNextNode(1);
                //Debug.Log("1");
                break;
            case YutResult.Gae:
                GameManager.Instance.mainGameProgress.currentCharacter.MoveToNextNode(2);
                //Debug.Log("2");
                break;
            case YutResult.Gur:
                GameManager.Instance.mainGameProgress.currentCharacter.MoveToNextNode(3);
                //Debug.Log("3");
                break;
            case YutResult.Yut:
                GameManager.Instance.mainGameProgress.currentCharacter.MoveToNextNode(4);
                //Debug.Log("4");
                break;
            case YutResult.Mo:
                GameManager.Instance.mainGameProgress.currentCharacter.MoveToNextNode(5);
                //Debug.Log("5");
                break;
        }
        
        //��ư ����
        Destroy(gameObject);
    }
}
