using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PlayerProfile : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI playerNameTmp;
    [SerializeField] TextMeshProUGUI characterNumber;

    public List<Color32> ProfileColor;
    public List<Sprite> ProfileSpriteList;
    public Transform CharacterIconList;
    public Image ProfileImage;
    public Image ProfileImageBackground;
    public ulong clientId;
    public FixedString128Bytes username;
    public int score;

    public void SetData(ulong clientId, FixedString128Bytes username, int score)
    {
        this.clientId = clientId;
        this.username = username;
        this.score = score;
        SetScoreIcon();
        SetColorAndImage();
        playerNameTmp.text = username.ToString();
        characterNumber.text = score + "/4";
    }

    private void SetColorAndImage()
    {
        ProfileImageBackground.color = ProfileColor[transform.GetSiblingIndex()];
        ProfileImage.sprite = ProfileSpriteList[transform.GetSiblingIndex()];
    }

    private void SetScoreIcon()
    {
        for (int i = 0; i < CharacterIconList.childCount; i++)
        {
            if (i < score)
            {
                Image icon = CharacterIconList.GetChild(i).GetComponent<Image>();
                icon.color = new Color32(140, 224, 54, 255);
            }
        }
    }
}
