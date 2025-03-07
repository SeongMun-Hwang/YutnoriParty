using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerProfile : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI playerNameTmp;
    [SerializeField] TextMeshProUGUI characterNumber;

    public ulong clientId;
    public FixedString128Bytes username;
    public int score;

    public void SetData(ulong clientId, FixedString128Bytes username, int score)
    {
        this.clientId = clientId;
        this.username = username;
        this.score = score;

        playerNameTmp.text = username.ToString();
    }
}
