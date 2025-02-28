using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class DevModeMover : NetworkBehaviour
{
    [SerializeField] TMP_InputField inputField;
    
    int num = 0;

    public void OnMovePressed()
    {
        //최대 9999까지
        if(ConvertToInt() && num < 10000)
        {
            MoveCharacter(num);
        }
        else
        {
            Debug.Log("잘못된 값입니다");
        }
    }

    void MoveCharacter(int n)
    {
        if (PlayerManager.Instance.isMoving)
        {
            GameManager.Instance.announceCanvas.ShowAnnounceText("Other Character is moving");
            return;
        }
        if (MainGameProgress.Instance.currentCharacter == null)
        {
            GameManager.Instance.announceCanvas.ShowAnnounceText("Choose Character First!", 2f);
            return;
        }

        GameManager.Instance.mainGameProgress.currentCharacter.MoveToNextNode(n);
    }

    //string에서 숫자로 변환 시도해보고 성공하면 true 반환
    bool ConvertToInt()
    {
        if(int.TryParse(inputField.text, out num))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public void OnPlusPressed()
    {
        if (ConvertToInt() && num < 10000)
        {
            num++;
            inputField.text = num.ToString();
        }
        else
        {
            Debug.Log("9999가 최대입니다");
        }
    }

    public void OnMinusPressed() 
    {
        if (ConvertToInt() && num > -1000)
        {
            num--;
            inputField.text = num.ToString();
        }
        else
        {
            Debug.Log("-999가 최대입니다");
        }
    }
}
