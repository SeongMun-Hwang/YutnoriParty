
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class YutResults : NetworkBehaviour
{
    [SerializeField] TextMeshProUGUI yutText;
    [SerializeField] List<Color32> yutColors;
    [SerializeField] List<string> yutNames;

    ulong targetId;

    public YutResult yutResult;

    public void SetYutText(YutResult result)
    {
        yutResult = result;
        GetComponent<Image>().color = yutColors[(int)result];
        yutText.text = yutNames[(int)result];
    }

    public void SetClientId(ulong id)
    {
        targetId = id;
    }

    public void OnButtonPressed()
    {
        AudioManager.instance.Playsfx(13);

        if (NetworkManager.Singleton.LocalClientId != targetId)
        {
            GameManager.Instance.announceCanvas.ShowAnnounceText("다른 플레이어의 턴입니다!");
            return;
        }
        if (PlayerManager.Instance.isMoving)
        {
            GameManager.Instance.announceCanvas.ShowAnnounceText("다른 말이 이동 중입니다!");
            return;
        }
        if (MainGameProgress.Instance.currentCharacter == null)
        {
            GameManager.Instance.announceCanvas.ShowAnnounceText("말을 선택하세요!", 2f);
            return;
        }
        if (YutManager.Instance.isCalulating)
        {
            GameManager.Instance.announceCanvas.ShowAnnounceText("윷 결과를 기다리세요", 2f);
            return;
        }
        if (EventNodeManager.Instance.checkingStepOn.Value)
        {
            GameManager.Instance.announceCanvas.ShowAnnounceText("Wait Event Excute", 2f);
            return;
        }
        if (EventNodeManager.Instance.islandBattleExcuting.Value)
        {
            GameManager.Instance.announceCanvas.ShowAnnounceText("Wait Event Excute", 2f);
            return;
        }
        if (GameManager.Instance.mainGameProgress.isEndMoveExcuting)
        {
            GameManager.Instance.announceCanvas.ShowAnnounceText("Wait EndMove Excute", 2f);
            return;
        }

        Debug.Log("이동 가능? : " + MainGameProgress.Instance.currentCharacter.GetComponent<CharacterInfo>().canMove.Value);
        //이동 못하는 애 골랐으면 다시 고르라고 안내함
        if (!MainGameProgress.Instance.currentCharacter.GetComponent<CharacterInfo>().canMove.Value)
        {
            GameManager.Instance.announceCanvas.ShowAnnounceText("무인도에 갇혔습니다!", 2f);
            if (PlayerManager.Instance.ReturnNumOfCharacter() == 1)
            {
                YutManager.Instance.RemoveYutResult(yutResult);
                if (YutManager.Instance.Results.Count == 0)
                {
                    MainGameProgress.Instance.EndMove();
                }
                Destroy(gameObject);
            }
            if(YutManager.Instance.Results.Count == 1 && yutResult==YutResult.BackDo)
            {
                YutManager.Instance.RemoveYutResult(yutResult);
                MainGameProgress.Instance.EndMove();
                Destroy(gameObject);
            }
            return;
        }
        ItemManager.Instance.RemoveItem();
        //버튼 누르면 윷 사라지게 함
        YutManager.Instance.HideYutRpc();
        
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

        //버튼 없앰
        //Destroy(gameObject);
        //DestroyYutResultRpc();
        
        YutManager.Instance.CallRemoveResultRpc(yutResult, NetworkManager.Singleton.LocalClientId); //다른 클라이언트에 동기화 콜
        YutManager.Instance.RemoveYutResult(yutResult); //본인거는 바로 없앰
    }

    [Rpc(SendTo.NotMe)]
    void DestroyYutResultRpc()
    {
        Debug.Log(gameObject);
    }
}
