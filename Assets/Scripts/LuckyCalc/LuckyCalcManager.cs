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
    private int timer = 10; // 턴 제한시간
    private Coroutine turnTimerCoroutine; // 턴 제한시간 타이머 코루틴

    // UI 관련
    public TMP_Text answerText;
    public TMP_Text opertorText;
    public TMP_Text leftOperandText;
    public TMP_Text rightOperandText;
    [SerializeField] private GameObject guidePanel;
    [SerializeField] private GameObject cardPrefabs;
    [SerializeField] private Transform cardParent;
    [SerializeField] private List<TMP_Text> usernameUI;
    [SerializeField] private TMP_Text turnText;
    [SerializeField] private TMP_Text timerUI;
    [SerializeField] private GameObject winMessageUI;
    [SerializeField] private GameObject loseMessageUI;

    // 게임 관련 변수
    public NetworkVariable<int> leftOperand = new NetworkVariable<int>(0);
    public NetworkVariable<int> operatorType = new NetworkVariable<int>(0);
    public NetworkVariable<int> rightOperand = new NetworkVariable<int>(0);
    public NetworkVariable<int> resultNumber = new NetworkVariable<int>(0);
    private int currentFlipCount = 0;
    private int[] flippedCardId = new int[2];

    public List<FlippableCard> cards = new List<FlippableCard>();
    [SerializeField] List<GameObject> playerObjects;

    private IOperatorStrategy operatorStrategy;

    //private bool isWaitForResult = false;
    public override void OnNetworkSpawn()
    {
        // 카드를 생성해서 로컬에서 관리
        for (int i = 0; i < 18; i++)
        {
            cards.Add(Instantiate(cardPrefabs, cardParent).GetComponent<FlippableCard>());
            cards[i].manager = this;
            cards[i].Id = i;
        }

        isPlaying.OnValueChanged += InitScoreBoardUI;
        currentTurnPlayerId.OnValueChanged += UpdateTurnUI;
        currentTurnPlayerName.OnValueChanged += UpdateTurnPlayerNameUI;
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

    public void InitScoreBoardUI(bool previousValue, bool newValue)
    {
        guidePanel.SetActive(false);
        for (int i = 0; i < playerIds.Count; i++)
        {
            usernameUI[i].transform.parent.gameObject.SetActive(true);
            foreach (PlayerProfileData data in GameManager.Instance.playerBoard.playerProfileDatas)
            {
                if (data.clientId == playerIds[i])
                {
                    usernameUI[i].text = data.userName.ToString();
                }
            }
        }
    }

    private void UpdateTurnPlayerNameUI(FixedString128Bytes previousValue, FixedString128Bytes newValue)
    {
        turnText.text = $"{newValue}";
    }

    private IEnumerator StartGameTimer(int timer = 3)
    {
        while (timer > 0)
        {
            yield return new WaitForSecondsRealtime(1f);
            timer--;

            if (timer == 0) break;
            GameManager.Instance.announceCanvas.ShowAnnounceTextClientRpc(timer.ToString(), 0.7f);
            yield return null;
        }

        GameManager.Instance.announceCanvas.ShowAnnounceTextClientRpc("카드를 뒤집어 수식을 완성하라!", 2f);

        int i = UnityEngine.Random.Range(0, playerIds.Count);
        currentTurnPlayerId.Value = playerIds[i];
        currentTurnPlayerName.Value = ServerSingleton.Instance.clientIdToUserData[playerIds[i]].userName;
        Debug.Log(currentTurnPlayerName.Value.ToString() + "에게 첫 턴");
        isPlaying.Value = true;
        StartTurnTimer();
        InitGame();
    }

    private void OnTurnChanged(ulong previousValue, ulong newValue)
    {
        Debug.Log("턴 변경");
        if (IsServer)
        {
            if (turnTimerCoroutine != null)
            {
                StopCoroutine(turnTimerCoroutine);
                turnTimerCoroutine = null;
            }

            StartTurnTimer();
        }
    }

    private void StartTurnTimer()
    {
        if (turnTimerCoroutine != null)
        {
            StopCoroutine(turnTimerCoroutine);
            turnTimerCoroutine = null;
        }

        turnTimerCoroutine = StartCoroutine(TurnTimerCoroutine());
    }

    private IEnumerator TurnTimerCoroutine()
    {
        int remainTime = timer;
        while (remainTime > 0)
        {
            // 클라이언트의 UI 업데이트를 위한 ClientRpc 호출
            UpdateTimerUIClientRpc(remainTime);
            yield return new WaitForSeconds(1f);
            remainTime -= 1;
        }
        // 제한 시간이 지나면 현재 턴을 자동 종료
        if (IsServer)
        {
            NextTurn();
        }
    }

    [ClientRpc]
    private void UpdateTimerUIClientRpc(int time)
    {
        timerUI.text = time.ToString();
    }

    public void NextTurn()
    {
        // 턴 종료 요청 시 타이머 코루틴 정지
        if (turnTimerCoroutine != null)
        {
            StopCoroutine(turnTimerCoroutine);
            turnTimerCoroutine = null;
        }

        int nextIndex = (playerIds.IndexOf(currentTurnPlayerId.Value) + 1) % playerIds.Count;

        currentTurnPlayerId.Value = playerIds[nextIndex]; // 턴 넘김
        currentTurnPlayerName.Value = ServerSingleton.Instance.clientIdToUserData[playerIds[nextIndex]].userName;

        leftOperand.Value = 0;
        rightOperand.Value = 0;
        CloseCardClientRpc(flippedCardId);
        currentFlipCount = 0;
        flippedCardId = new int[2];

        // 새 턴 시작 시 타이머를 다시 시작
        StartTurnTimer();
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
        if (newValue == 0)
        {
            leftOperandText.text = "?";
        }
        else
        {
            leftOperandText.text = newValue.ToString();
        }
    }

    private void UpdateRightOperand(int previousValue, int newValue)
    {
        if (newValue == 0)
        {
            rightOperandText.text = "?";
        }
        else
        {
            rightOperandText.text = newValue.ToString();
        }
    }

    private void UpdateTurnUI(ulong previousValue, ulong newValue)
    {
        for (int i = 0; i < playerIds.Count; i++)
        {
            int order = GameManager.Instance.GetOrderOfPlayerById(playerIds[i]);
            playerObjects[order].SetActive(true);
        }

        for (int i = 0; i < playerIds.Count; i++)
        {
            if (GetCurrentTurnPlayerId() == playerIds[i])
            {
                int order = GameManager.Instance.GetOrderOfPlayerById(playerIds[i]);

                if (order != -1)
                {
                    usernameUI[i].color = Color.yellow;
                    playerObjects[order].transform.GetChild(0).GetComponent<Animator>().SetTrigger("MyTurn");
                }
            }
            else
            {
                int order = GameManager.Instance.GetOrderOfPlayerById(playerIds[i]);

                if (order != -1)
                {
                    usernameUI[i].color = Color.white;
                    playerObjects[order].transform.GetChild(0).GetComponent<Animator>().SetTrigger("NotMyTurn");
                }
            }
        }
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
        if (currentFlipCount >= 2) return;

        flippedCardId[currentFlipCount] = cardId;
        currentFlipCount++;

        if (currentFlipCount == 1)
        {
            leftOperand.Value = cards[cardId].Number;
            OpenCardClientRpc(cardId);
        }
        else if (currentFlipCount == 2)
        {
            rightOperand.Value = cards[cardId].Number;
            OpenCardClientRpc(cardId);

            StartCoroutine(CheckResult());
        }
    }

    [ClientRpc]
    private void WrongAnswerClientRpc()
    {
        AudioManager.instance.Playsfx(11);
    }

    private IEnumerator CheckResult()
    {
        
        yield return new WaitForSecondsRealtime(1f);

        if (operatorStrategy.Calc(leftOperand.Value, rightOperand.Value, resultNumber.Value))
        {
            Debug.Log("정답!");
            MainGameProgress.Instance.winnerId = currentTurnPlayerId.Value;
            GameFinishedClientRpc(currentTurnPlayerId.Value);
            StartCoroutine(PassTheScene());
        }
        else
        {
            Debug.Log("오답...");
            WrongAnswerClientRpc();
            NextTurn();
        }
    }

    [ClientRpc]
    private void OpenCardClientRpc(int cardId)
    {
        cards[cardId].OpenCard();
        AudioManager.instance.Playsfx(9);
    }

    [ClientRpc]
    private void CloseCardClientRpc(int[] cardIds)
    {
        for (int i = 0; i < cards.Count; i++)
        {
            cards[i].CloseCard();
        }
    }

    public ulong GetCurrentTurnPlayerId()
    {
        return currentTurnPlayerId.Value;
    }

    [ClientRpc]
    public void GameFinishedClientRpc(ulong winClientId)
    {
        AudioManager.instance.Playsfx(10);
        if (MinigameManager.Instance.playerType != Define.MGPlayerType.Player) { return; }

        if (NetworkManager.Singleton.LocalClientId == winClientId)
        {
            winMessageUI.SetActive(true);
        }
        else
        {
            loseMessageUI.SetActive(true);
        }

        Debug.Log("게임 종료");
    }

    public IEnumerator PassTheScene()
    {
        yield return new WaitForSecondsRealtime(2f);
        MinigameManager.Instance.EndMinigame();
    }
}
