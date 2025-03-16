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
        SetColorAndImage();
        playerNameTmp.text = username.ToString();
        characterNumber.text = score + "/4";
    }

    private void SetColorAndImage()
    {
        ProfileImageBackground.color = ProfileColor[transform.GetSiblingIndex()];
        ProfileImage.sprite = ProfileSpriteList[transform.GetSiblingIndex()];
    }
}
