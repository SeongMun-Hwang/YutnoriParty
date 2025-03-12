using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class LuckyCalcManager : NetworkBehaviour
{
    // 게임에 참여하는 유저 관련
    private NetworkList<ulong> playerIds = new NetworkList<ulong>(); // 참가한 플레이어 ID 리스트

    [SerializeField] public NetworkVariable<bool> isPlaying;

    [SerializeField] public NumberPanel leftNumberPanel;
    [SerializeField] public NumberPanel rightNumberPanel;
    [SerializeField] public TMP_Text goldenCardText;
    [SerializeField] public TMP_Text opertorCardText;

    private void Start()
    {
        leftNumberPanel.MakeCardDeck(new Color32(176, 159, 109, 255));
        rightNumberPanel.MakeCardDeck(new Color32(151, 109, 176, 255));

        int a = Random.Range(1, 10);
        int b = Random.Range(1, 10);
        int op = Random.Range(0, 3);
        int resultNumber = 0;

        switch (op)
        {
            case 0:
                resultNumber = a + b;
                opertorCardText.text = "+<br><sub>Plus";
                break;
            case 1:
                resultNumber = a - b;
                opertorCardText.text = "-<br><sub>Minus";
                break;
            case 2:
                resultNumber = a * b;
                opertorCardText.text = "X<br><sub>Multiply";
                break;
        }

        goldenCardText.text = resultNumber.ToString();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            foreach (var clientPair in NetworkManager.Singleton.ConnectedClients)
            {
                ulong clientId = clientPair.Key;
                OnPlayerJoined(clientId);
            }
        }
    }

    private void OnPlayerJoined(ulong clientId)
    {
        if (!playerIds.Contains(clientId) && MinigameManager.Instance.IsPlayer(clientId))
        {
            playerIds.Add(clientId);
        }

        if (playerIds.Count == MinigameManager.Instance.maxPlayers.Value)
        {
            StartCoroutine(StartGameTimer(5));
        }
    }

    private IEnumerator StartGameTimer(int timer = 3)
    {
        while (timer > 0)
        {
            yield return new WaitForSecondsRealtime(1f);
            timer--;
            GameManager.Instance.announceCanvas.ShowAnnounceTextClientRpc(timer.ToString(), 0.7f);
            yield return null;
        }

        GameManager.Instance.announceCanvas.ShowAnnounceTextClientRpc("Start!", 1f);

        // 게임 시작 시 랜덤한 플레이어가 첫 턴을 가짐
        isPlaying.Value = true;
    }
}
