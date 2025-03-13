using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class LuckyCalcManager : NetworkBehaviour
{
    // 게임에 참여하는 유저 관련
    private NetworkList<ulong> playerIds = new NetworkList<ulong>(); // 참가한 플레이어 ID 리스트

    // 현재 차례 플레이어 ID
    public NetworkVariable<ulong> currentTurnPlayerId = new NetworkVariable<ulong>(100);
    private NetworkVariable<FixedString128Bytes> currentTurnPlayerName = new NetworkVariable<FixedString128Bytes>(".");

    [SerializeField] public NetworkVariable<bool> isPlaying;

    // UI 관련
    public TMP_Text answerText;
    public TMP_Text opertorText;
    public TMP_Text leftOperandText;
    public TMP_Text rightOperandText;
    [SerializeField] private GameObject cardPrefabs;
    [SerializeField] private Transform cardParent;

    // 게임 관련 변수
    public NetworkVariable<int> leftOperand = new NetworkVariable<int>(0);
    public NetworkVariable<int> operatorType = new NetworkVariable<int>(0);
    public NetworkVariable<int> rightOperand = new NetworkVariable<int>(0);
    public NetworkVariable<int> resultNumber = new NetworkVariable<int>(0);
    private int currentFlipCount = 0;
    private int[] flippedCardId = new int[2];

    public List<FlippableCard> cards = new List<FlippableCard>();

    private IOperatorStrategy operatorStrategy;

    private bool isWaitForResult = false;
    public override void OnNetworkSpawn()
    {
        // 카드를 생성해서 로컬에서 관리
        for (int i = 0; i < 18; i++)
        {
            cards.Add(Instantiate(cardPrefabs, cardParent).GetComponent<FlippableCard>());
            cards[i].manager = this;
            cards[i].Id = i;
        }

        currentTurnPlayerId.OnValueChanged += UpdateTurnUI;
        leftOperand.OnValueChanged += UpdateLeftOperand;
        rightOperand.OnValueChanged += UpdateRightOperand;

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

        int i = UnityEngine.Random.Range(0, playerIds.Count);
        currentTurnPlayerId.Value = playerIds[i];
        currentTurnPlayerName.Value = ServerSingleton.Instance.clientIdToUserData[playerIds[i]].userName;
        Debug.Log(currentTurnPlayerName.Value.ToString() + "에게 첫 턴");
        isPlaying.Value = true;
        InitGame();
    }

    private void InitGame()
    {
        if (!IsServer) return;

        // 서버에서 랜덤으로 연산기호와 결과값을 만들어 냄
        int a = Random.Range(1, 10);
        int b = Random.Range(1, 10);
        int op = Random.Range(0, 3);
        FixedString128Bytes opString = new FixedString128Bytes();
        switch (op)
        {
            case 0:
                resultNumber.Value = a + b;
                operatorStrategy = new AddOperatorStrategy();
                opString = "+";
                break;
            case 1:
                resultNumber.Value = a - b;
                operatorStrategy = new SubtractOperatorStrategy();
                opString = "-";
                break;
            case 2:
                resultNumber.Value = a * b;
                operatorStrategy = new MultiplyOperatorStrategy();
                opString = "×";
                break;
        }

        // 카드를 셔플해서 1~9까지 각각 2장씩 갖도록 함
        List<int> cardNumbers = new List<int>();
        for (int i = 1; i <= 9; i++)
        {
            cardNumbers.Add(i);
            cardNumbers.Add(i);
        }
        Shuffle(cardNumbers);

        InitGameClientRpc(opString, resultNumber.Value, cardNumbers.ToArray());
    }

    private void Shuffle(List<int> list)
    {
        System.Random rand = new System.Random();
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rand.Next(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    [ClientRpc]
    private void InitGameClientRpc(FixedString128Bytes opText, int result, int[] cardNumbers)
    {
        opertorText.text = opText.ToString();
        answerText.text = result.ToString();

        for (int i = 0; i < cards.Count; i++)
        {
            cards[i].Number = cardNumbers[i];
        }
    }

    private void Update()
    {

    }

    private void UpdateLeftOperand(int previousValue, int newValue)
    {
        leftOperandText.text = newValue.ToString();
    }

    private void UpdateRightOperand(int previousValue, int newValue)
    {
        rightOperandText.text = newValue.ToString();
    }

    private void UpdateTurnUI(ulong previousValue, ulong newValue)
    {
        
    }

    public void TryToFlip(int cardId)
    {
        if (GetCurrentTurnPlayerId() != NetworkManager.Singleton.LocalClientId)
        {
            Debug.Log("당신 턴 아님");
            return;
        }

        OpenCardServerRpc(cardId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void OpenCardServerRpc(int cardId)
    {
        flippedCardId[currentFlipCount] = cardId;
        currentFlipCount++;

        if (currentFlipCount == 1)
        {
            leftOperand.Value = cards[cardId].Number;
        }
        else if (currentFlipCount == 2)
        {
            rightOperand.Value = cards[cardId].Number;

            // TODO : 조금 기다렸다가 결과 출력하고 턴 넘기기, 다시 카드 되돌리기
        }

        OpenCardClientRpc(cardId);
    }

    [ClientRpc]
    private void OpenCardClientRpc(int cardId)
    {
        cards[cardId].OpenCard();
    }

    public ulong GetCurrentTurnPlayerId()
    {
        return currentTurnPlayerId.Value;
    }
}
