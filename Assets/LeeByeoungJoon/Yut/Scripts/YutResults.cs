
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
        //버튼 누르면 윷 사라지게 함
        YutManager.Instance.HideYutRpc();

        if (PlayerManager.Instance.isMoving)
        {
            GameManager.Instance.announceCanvas.ShowAnnounceText("Other Character is moving");
            return;
        }
        if (MainGameProgress.Instance.currentCharacter==null)
        {
            GameManager.Instance.announceCanvas.ShowAnnounceText("Choose Character First!",2f);
            return;
        }

        //이동 못하는 애 골랐으면 다시 고르라고 안내함
        if (!MainGameProgress.Instance.currentCharacter.GetComponent<CharacterInfo>().canMove)
        {
            GameManager.Instance.announceCanvas.ShowAnnounceText("This Character Cannot Move!", 2f);
            return;
        }
        //몇 칸 전진하는지 숫자 반환
        switch (yutResult)
        {
            case YutResult.BackDo:
                GameManager.Instance.mainGameProgress.currentCharacter.MoveToNextNode(-1);
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
        //결과 리스트에서 뺌
        //Debug.Log("네트워크 싱글톤 id " + NetworkManager.Singleton.LocalClientId);
        YutManager.Instance.RemoveYutResult(yutResult);

        //버튼 없앰
        Destroy(gameObject);
    }
}
