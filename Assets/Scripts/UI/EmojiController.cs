using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class EmojiController : MonoBehaviour
{
    public GameObject EmojiButtonPrefabs;
    private List<Button> EmojiButtonList;
    private bool isDelay = false;
    private float delayDuration = 6f;

    private void Start()
    {
        EmojiButtonList = new List<Button>();
        for (int i = 0; i < 8; i++)
        {
            int index = i;
            Button b = Instantiate(EmojiButtonPrefabs, transform).GetComponent<Button>();
            b.image.sprite = GameManager.Instance.emojiList[i];
            b.onClick.AddListener(() =>
            {
                if (!GameManager.Instance.isEmojiDelay)
                {
                    SendEmoji(NetworkManager.Singleton.LocalClientId, index);
                    GameManager.Instance.isEmojiDelay = true;
                    GameManager.Instance.HandleEmojiDelay(delayDuration);
                }
                else
                {
                    GameManager.Instance.announceCanvas.ShowAnnounceText("잠시동안 이모지를 보낼 수 없습니다", 1f);
                }
            });
            EmojiButtonList.Add(b);
        }
    }

    public void SendEmoji(ulong id, int emojiCode)
    {
        GameManager.Instance.playerBoard.DrawEmojiServerRpc(id, emojiCode);
    }
}
