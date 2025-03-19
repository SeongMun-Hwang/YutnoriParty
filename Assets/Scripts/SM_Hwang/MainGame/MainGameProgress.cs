using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class MainGameProgress : NetworkBehaviour
{
    private int numOfPlayer;
    private NetworkVariable<int> currentPlayerNumber = new NetworkVariable<int>(0);
    private NetworkVariable<int> gameTurn = new NetworkVariable<int>(0);
    public CharacterBoardMovement currentCharacter;
    private GameObject encounteredEnemy;
    public Camera maingameCamera;
    private static MainGameProgress instance;
    public static MainGameProgress Instance { get { return instance; } }
    public System.Action endMinigameActions;
    public ulong winnerId;
    bool _isMinigamePlaying = false;
    public NetworkVariable<bool> isGameEnd = new NetworkVariable<bool>(false);
    public bool isMinigamePlaying
    {
        get => _isMinigamePlaying;
        set
        {
            Debug.Log($"isMinigamePlaying 값 변경 : {isMinigamePlaying} -> {value}");
            _isMinigamePlaying = value;
        }
    }
    public bool isEndMoveExcuting = false;

    [SerializeField] bool isWaitMinigameEnd = false;
    bool _isEventChecking;
    public bool isEventChecking
    {
        get => _isEventChecking;
        set
        {
            Debug.Log($"isEventChecking 값 변경 : {_isEventChecking} -> {value}");
            _isEventChecking = value;
        }
    }

    private void Update()
    {
        ChooseCharacter();
    }
    public override void OnNetworkSpawn()
    {
        instance = this;
    }

    public override void OnNetworkDespawn()
    {

    }

    /*게임 시작*/
    //시작 시 입장한 플레이어 수 저장 및 랜덤으로 시작턴 지정
    public void StartGame()
    {
        numOfPlayer = NetworkManager.ConnectedClients.Count;
        currentPlayerNumber.Value = UnityEngine.Random.Range(0, numOfPlayer);
        YutManager.Instance.HideYutRpc(); //윷 안보이게 함
        StartTurn((int)NetworkManager.ConnectedClientsIds[currentPlayerNumber.Value]);
    }
    /*턴 시작*/
    //누구의 턴인지 공지
    void StartTurn(int n)
    {
        Debug.Log("Start Turn");
        GameManager.Instance.playerBoard.SetProfileOutlineClientRpc(NetworkManager.ConnectedClientsIds[currentPlayerNumber.Value]);
        GameManager.Instance.announceCanvas.ShowAnnounceTextClientRpc(PlayerManager.Instance.RetrunPlayerName((ulong)n)+ "턴!", 2f);
        SpawnInGameCanvasClientRpc(new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { (ulong)n } } });
    }
    /*UI 소환*/
    //현재 턴인 클라이언트에 게임 진행을 위한 캔버스 액티브, 던지기 기회++
    [ClientRpc]
    public void SpawnInGameCanvasClientRpc(ClientRpcParams clientRpcParams = default)
    {
        Debug.Log("Spawn canvas client rpc");
        GameManager.Instance.inGameCanvas.SetActive(true);
        YutManager.Instance.throwChance++;
        //StartCoroutine(WaitForCanvasAndActivate());
    }
    /*이동 종료 함수*/
    //말이 윷 결과에 따른 이동을 마쳤을 때마다 호출
    //더 이상 던질 기회와 이동 가능한 결과가 없으면 턴 종료
    public void EndMove()
    {
        StartCoroutine(EndMoveCoroutine());
    }

    public IEnumerator EndMoveCoroutine()
    {
        isEndMoveExcuting = true;

        Debug.Log("EndMove 호출 : " + NetworkManager.Singleton.LocalClientId);

        isEventChecking = true;
        bool subscribed = false;
        //여기인듯? 위에거 실행 끝날때까지 안기다려버림
        //밟았는지 체킹 중에는 대기
        //int timeOut = 10;
        while (isEventChecking)
        {
            if (!subscribed)
            {
                //EventNodeManager.Instance.checkingStepOn.OnValueChanged += OnCheckingStepOnChangedServerRpc;
                EventNodeManager.Instance.CheckStepOnServerRpc(); //이동 끝나고 노드 밟았는지 체크
                subscribed = true;
            }

            yield return new WaitForSecondsRealtime(1);
            //isEventChecking = EventNodeManager.Instance.checkingStepOn.Value;
            Debug.Log("EndMove : 이벤트 노드 검사중? : " + isEventChecking);

            //timeOut--;
            //Debug.Log("EndMove : 이벤트 노드 실행 기다리는 중 : " + timeOut);

            //if (timeOut == 0)
            //{
            //    Debug.Log("EndMove 타임아웃");
            //    break;
            //}

            if (!isEventChecking)
            {
                break;
            }
        }
        //EventNodeManager.Instance.checkingStepOn.OnValueChanged -= OnCheckingStepOnChangedServerRpc;

        Debug.Log("EndMove 이벤트 노드 실행 대기 끝");
        Debug.Log("미니게임 플레이중? : " + isMinigamePlaying);

        if (EventNodeManager.Instance.checkingStepOn.Value)
        {
            Debug.Log("이벤트 노드 실행중이지만 타임아웃으로 빠져나옴");
        }

        if (CheckOtherPlayer()) //이동이 끝나고 주변 체크
        {
            StartMiniGame(encounteredEnemy);
        }
        StartCoroutine(WaitUntilMinigameEnd()); //미니게임 끝날 때까지 대기

        yield return null;
    }

    [ServerRpc(RequireOwnership = false)]
    void OnCheckingStepOnChangedServerRpc(bool previous, bool current)
    {
        Debug.Log("노드 밟았는지? 이전 값 : " + previous + " 현재 값 : " + current);

        //isEventChecking = current;
        ChangeEventCheckingStateClientRpc();
    }

    [ClientRpc]
    void ChangeEventCheckingStateClientRpc()
    {
        isEventChecking = EventNodeManager.Instance.checkingStepOn.Value;
        Debug.Log("클라이언트 이벤트 체킹 변경\n이벤트 노드 매니저 값 : " + EventNodeManager.Instance.checkingStepOn.Value + "\nisEventChecking 값 : " + isEventChecking);
    }
    [ClientRpc]
    public void ChangeEventCheckingClientRpc(bool value)
    {
        isEventChecking = value;
        Debug.Log("클라이언트 이벤트 체킹 변경\nisEventChecking 값 : " + isEventChecking);
    }
    /*이동 종료 후 같은 위치의 적 탐색*/
    private bool CheckOtherPlayer()
    {
        Debug.Log("다른 캐릭터 겹치는지 체크");

        if (currentCharacter == null) return false;
        Collider[] hitColliders = Physics.OverlapSphere(currentCharacter.transform.position, 1f);
        foreach (Collider collider in hitColliders)
        {
            if (collider.gameObject == currentCharacter.gameObject) continue;
            if (collider.TryGetComponent<CharacterBoardMovement>(out var character))
            {
                Debug.Log("compare id : " + character.GetComponent<NetworkObject>().OwnerClientId + " | " + NetworkManager.Singleton.LocalClientId);
                if (character.GetComponent<NetworkObject>().OwnerClientId != NetworkManager.Singleton.LocalClientId) //적이면
                {
                    encounteredEnemy = character.gameObject;
                    return true;
                }
                else if (character.GetComponent<NetworkObject>().OwnerClientId == NetworkManager.Singleton.LocalClientId)//내 말이면
                {
                    if (character.GetComponent<CharacterBoardMovement>().currentNode != currentCharacter.GetComponent<CharacterBoardMovement>().currentNode)
                    {
                        Debug.Log("말 업기 실패 - 위치가 같지 않음 ");
                        return false;
                    }
                    PlayerManager.Instance.OverlapCharacter(character.gameObject, currentCharacter.gameObject);
                    currentCharacter.GetComponent<Outline>().DisableOutline();
                    currentCharacter = character;
                    character.GetComponent<Outline>().EnableOutline();
                    return false;
                }
            }
        }
        return false;
    }
    /*미니게임 진행 중, isMinigamePlaying==true일 동안 대기*/
    private IEnumerator WaitUntilMinigameEnd()
    {
        isWaitMinigameEnd = true;
        Debug.Log("미니게임 끝날때까지 대기");

        while (isMinigamePlaying)
        {
            yield return null;
        }
        yield return new WaitForSeconds(1f);
        if (YutManager.Instance.YutResultCount() == 0 && !PlayerManager.Instance.isMoving
            && YutManager.Instance.throwChance == 0 && !YutManager.Instance.isCalulating)
        {
            CheckAllPlayerTurnPassed();
        }
        else
        {
            isEndMoveExcuting = false;
            Debug.Log("할거 남음");
        }

        isWaitMinigameEnd = false;
    }
    /*모든 유저 턴
     * 이 한 번 씩 진행됐는지 확인*/
    //다 지나갔으면 파티게임 진행
    private void CheckAllPlayerTurnPassed()
    {
        Debug.Log("모든 플레이어 턴 했는지 체크");
        //gameTurn은 0부터 시작, gameTurn+1이 전체 참여자 수의 배수일 때 파티게임 진행
        if ((gameTurn.Value + 1) % NetworkManager.ConnectedClients.Count == 0)
        {
            Debug.Log("Party game start");
            GameManager.Instance.announceCanvas.ShowAnnounceTextClientRpc("파티 타임!");
            StartMiniGame();
            StartCoroutine(WaitUntilPartygameEnd()); //파티게임 끝나기 대기(미니게임과 동일)
        }
        //파티할 시간이 아니면 턴 변경할 지 확인
        else
        {
            CheckTurnChange();
        }
    }
    /*파티 게임 끝날 때까지 대기 후 턴 변경 체크*/
    private IEnumerator WaitUntilPartygameEnd()
    {
        while (isMinigamePlaying)
        {
            yield return null;
        }
        yield return new WaitForSeconds(1f);
        CheckTurnChange();
    }
    /*턴 변경 체크*/
    private void CheckTurnChange()
    {
        Debug.Log("턴 변경 체크");

        if (YutManager.Instance.YutResultCount() == 0 && YutManager.Instance.throwChance == 0
            && !YutManager.Instance.isCalulating)
        {
            Debug.Log("End turn");
            GameManager.Instance.inGameCanvas.SetActive(false);
            DestoryCharacterOnStartNode();
            EndTurnServerRpc();
        }

        //IsEndMoveExcuteChangeServerRpc(false);
        isEndMoveExcuting = false;
    }
    /*턴 종료*/
    //다음 플레이어의 턴 시작, 이상 반복
    [ServerRpc(RequireOwnership = false)]
    void EndTurnServerRpc()
    {
        EventNodeManager.Instance.TurnCountServerRpc(); //이벤트 노드 턴 계산

        Debug.Log("Change Turn");
        gameTurn.Value++;
        currentPlayerNumber.Value++;
        if (currentPlayerNumber.Value == numOfPlayer)
        {
            currentPlayerNumber.Value = 0;
        }
        StartTurn((int)NetworkManager.ConnectedClientsIds[currentPlayerNumber.Value]);
    }
    // 미니게임 시작하기 위해 움직이는 말이 감지한 적과 함께 호출
    private void StartMiniGame(GameObject enemy = null)
    {
        Debug.Log("미니게임 시작");

        isMinigamePlaying = true;
        if (enemy != null)
        {
            // 각 말의 NetworkObject를 추출
            if (!currentCharacter.gameObject.TryGetComponent<NetworkObject>(out var playerNetObj) || !playerNetObj.IsSpawned)
            {
                Debug.LogError("attacker는 NetworkObject가 아니거나 아직 Spawn되지 않았습니다!");
                return;
            }

            if (!enemy.TryGetComponent<NetworkObject>(out var enemyNetObj) || !enemyNetObj.IsSpawned)
            {
                Debug.LogError("enemy는 NetworkObject가 아니거나 아직 Spawn되지 않았습니다!");
                return;
            }

            // 각 말의 NetworkObjectReference를 보내 서버에 미니게임 실행 요청
            StartMiniGameServerRpc(new NetworkObjectReference(playerNetObj), new NetworkObjectReference(enemyNetObj));
        }
        else
        {
            StartPartyGameServerRpc();
        }
    }

    [ClientRpc]
    void ChangeMinigamePlayingClientRpc(bool value)
    {
        isMinigamePlaying = value;
        Debug.Log("isMinigamePlaying : " + isMinigamePlaying);
    }

    [ServerRpc(RequireOwnership = false)]
    public void StartBlackHoleMiniGameServerRpc(NetworkObjectReference node, NetworkObjectReference triggeredCharacter, ulong[] playerIds, NetworkObjectReference[] characterList)
    {
        Debug.Log("여러명 미니게임 시작");
        ChangeMinigamePlayingClientRpc(true);

        if (!node.TryGet(out NetworkObject no))
        {
            Debug.Log("네트워크 오브젝트 못찾음");
        }

        endMinigameActions = null;
        endMinigameActions += (() =>
        {
            EndMiniGameClientRpc();

            List<NetworkObjectReference> winnerCharacters = new List<NetworkObjectReference>();

            //미니 게임 승자 판별
            GameManager.Instance.announceCanvas.ShowAnnounceTextClientRpc(PlayerManager.Instance.RetrunPlayerName(winnerId) + "승리!", 2f);
            //캐릭터 처리
            foreach (var characterRef in characterList)
            {
                if (!characterRef.TryGet(out NetworkObject character)) continue;

                //승자의 캐릭터가 아니면 다 디스폰
                if (character.OwnerClientId != winnerId)
                {
                    PlayerManager.Instance.DespawnCharacterServerRpc(character, character.OwnerClientId);
                    Debug.Log("캐릭터 id : " + character.NetworkObjectId);
                }
                else//승자 캐릭터는 리스트로 따로 추출
                {
                    winnerCharacters.Add(character);
                }
            }

            //승자의 캐릭터가 둘 이상일때 쌓기 실행
            if (winnerCharacters.Count >= 2)
            {
                StackCharactersClientRpc(winnerCharacters.ToArray(), new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new List<ulong> { winnerId }
                    }
                });
            }

            if (!triggeredCharacter.TryGet(out NetworkObject triggered))
            {
                Debug.Log("네트워크 오브젝트 못찾음");
            }

            //CheckTurnChangeClientRpc(new ClientRpcParams
            //{
            //    Send = new ClientRpcSendParams
            //    {
            //        TargetClientIds = new List<ulong> { triggered.OwnerClientId }
            //    }
            //});

            //미니게임 끝나고서야 이벤트 종료 호출
            no.GetComponent<BlackHoleNode>().BlackHoleEventEndRpc();
        });
        MinigameManager.Instance.SetPlayers(playerIds);
        MinigameManager.Instance.StartMinigame();
        //StartMiniGameClientRpc();
    }

    [ClientRpc]
    void CheckTurnChangeClientRpc(ClientRpcParams rpcParams)
    {
        Debug.Log("블랙홀 밟은 놈이 턴 종료 체크");
        StartCoroutine(WaitUntilPartygameEnd());
    }

    //플레이어 매니저가 모두 달라 특정 클라이언트만 지정
    //캐릭터 목록을 받아 쌓기
    [ClientRpc]
    void StackCharactersClientRpc(NetworkObjectReference[] arr, ClientRpcParams rpcParams)
    {
        //네트워크 오브젝트 레퍼런스 -> 게임 오브젝트 리스트로 변환
        List<GameObject> characters = new List<GameObject>();
        for (int i = 0; i < arr.Length; i++)
        {
            arr[i].TryGet(out NetworkObject character);
            characters.Add(character.gameObject);
        }

        for (int i = 1; i < characters.Count; i++)
        {
            PlayerManager.Instance.OverlapCharacter(characters[0], characters[i]); //겹치고
            characters[i].GetComponent<Outline>().DisableOutline(); //위에 있는 애 아웃라인 끄고
        }

        currentCharacter = characters[0].GetComponent<CharacterBoardMovement>(); //현재 캐릭터 제일 밑에 있는애로 바꿔주고
        characters[0].GetComponent<Outline>().EnableOutline(); //밑에 있는 애 아웃라인 켜고
    }

    /*캐릭터 선택*/
    //Ray를 통해 이동할 말 선택
    //내 말이 아니면 메시지 출력
    public void ChooseCharacter()
    {
        if (isMinigamePlaying || PlayerManager.Instance.isMoving) return;
        if (Input.GetMouseButtonDown(0))
        {
            if (Camera.main != null)
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit))
                {
                    if (hit.collider.TryGetComponent<NetworkObject>(out var networkObject) &&
                        networkObject.OwnerClientId != NetworkManager.LocalClientId)
                    {
                        Debug.Log("본인 캐릭터가 아닙니다!");
                        return;
                    }
                    if (hit.collider.gameObject.TryGetComponent<CharacterBoardMovement>(out var character))
                    {
                        if (currentCharacter != null)
                        {
                            currentCharacter.GetComponent<Outline>().DisableOutline();
                        }
                        hit.collider.gameObject.GetComponent<Outline>().EnableOutline();
                        currentCharacter = character;
                    }
                }
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void StartMiniGameServerRpc(NetworkObjectReference playerReference, NetworkObjectReference enemyReference)
    {
        Debug.Log("MiniGame Start!");
        // 각 NetworkObjectReference에서 GameObject 추출
        if (!playerReference.TryGet(out NetworkObject playerNetObj))
        {
            Debug.LogError("attacker NetworkObject가 없습니다!");
            return;
        }

        if (!enemyReference.TryGet(out NetworkObject enemyNetObj))
        {
            Debug.LogError("enemy NetworkObject가 없습니다!");
            return;
        }

        GameObject player = playerNetObj.gameObject;
        GameObject enemy = enemyNetObj.gameObject;

        //하나라도 섬에 있으면 섬전투로 취급
        bool isIslandBattle = (playerNetObj.GetComponent<CharacterInfo>().inIsland.Value || enemyNetObj.GetComponent<CharacterInfo>().inIsland.Value);
        Debug.Log("섬 전투임? : " + isIslandBattle);

        if (isIslandBattle)
        {
            EventNodeManager.Instance.islandBattleExcuting.Value = true;
        }

        // 미니 게임이 끝났을 때 서버에서 발생시킬 이벤트를 지정
        endMinigameActions = null;
        endMinigameActions += (() =>
        {
            EndMiniGameClientRpc();

            //무승부나면 둘 다 사망
            if(winnerId == 99)
            {
                Debug.Log("Attacker Draw / Enemy Draw");
                ulong playerId = playerNetObj.OwnerClientId;
                ulong enemyId = enemyNetObj.OwnerClientId;

                if (isIslandBattle)
                {
                    EventNodeManager.Instance.EscapeIslandCallRpc(playerNetObj);
                    EventNodeManager.Instance.EscapeIslandCallRpc(enemyNetObj);
                }

                GameManager.Instance.announceCanvas.ShowAnnounceTextClientRpc("무승부!", 2f);

                PlayerManager.Instance.DespawnCharacterServerRpc(player, playerId);
                PlayerManager.Instance.DespawnCharacterServerRpc(enemy, enemyId);

                if (isIslandBattle)
                {
                    EventNodeManager.Instance.islandBattleExcuting.Value = false;
                }

                return;
            }

            //미니 게임 승자 판별과 패배한 말 처리
            GameManager.Instance.announceCanvas.ShowAnnounceTextClientRpc(PlayerManager.Instance.RetrunPlayerName(winnerId) + "승리!", 2f);
            if (winnerId == playerNetObj.OwnerClientId)
            {
                Debug.Log("Attacker Win / Enemy Lose");

                AddThrowChanceClientRpc(winnerId);
                if (isIslandBattle)
                {
                    EventNodeManager.Instance.EscapeIslandCallRpc(playerNetObj);
                    EventNodeManager.Instance.EscapeIslandCallRpc(enemy.GetComponent<NetworkObject>());
                }
                PlayerManager.Instance.DespawnCharacterServerRpc(enemy, enemy.GetComponent<NetworkObject>().OwnerClientId);
                if (isIslandBattle)
                {
                    EventNodeManager.Instance.islandBattleExcuting.Value = false;
                    return;
                }
            }
            else
            {
                Debug.Log("Attacker Lose / Enemy Win");

                //승자가 섬을 바로 탈출
                if (isIslandBattle)
                {
                    EventNodeManager.Instance.EscapeIslandCallRpc(enemyNetObj);
                    EventNodeManager.Instance.EscapeIslandCallRpc(player.GetComponent<NetworkObject>());
                }
                PlayerManager.Instance.DespawnCharacterServerRpc(player, player.GetComponent<NetworkObject>().OwnerClientId);
                if (isIslandBattle)
                {
                    EventNodeManager.Instance.islandBattleExcuting.Value = false;
                    return;
                }
            }
        });
        ulong[] players = new ulong[2] { playerNetObj.OwnerClientId, enemyNetObj.OwnerClientId };
        MinigameManager.Instance.SetPlayers(players);
        MinigameManager.Instance.StartMinigame();
    }
    [ServerRpc(RequireOwnership = false)]
    void StartPartyGameServerRpc()
    {
        Debug.Log("PartyGame Start!");
        isMinigamePlaying = true;
        List<ulong> playerIds = new List<ulong>();

        foreach (var client in NetworkManager.Singleton.ConnectedClients)
        {
            ulong clientId = client.Key;
            playerIds.Add(clientId);
        }
        endMinigameActions = null;
        endMinigameActions += (() =>
        {
            EndMiniGameClientRpc();

            //미니 게임 승자 판별과 패배한 말 처리
            if (winnerId != 99)
            {
                GameManager.Instance.announceCanvas.ShowAnnounceTextClientRpc(PlayerManager.Instance.RetrunPlayerName(winnerId) + "승리!", 2f);
            }
            else
            {
                GameManager.Instance.announceCanvas.ShowAnnounceTextClientRpc("무승부!", 2f);
            }
            ItemManager.Instance.GetItemClientRpc(winnerId);
        });
        MinigameManager.Instance.SetPlayers(playerIds.ToArray());
        MinigameManager.Instance.StartMinigame();
    }
    [ClientRpc]
    void AddThrowChanceClientRpc(ulong targetId)
    {
        if (targetId == NetworkManager.Singleton.LocalClientId)
        {
            Debug.Log("기회 얻음");
            YutManager.Instance.throwChance++;
        }
    }
    [ClientRpc]
    void EndMiniGameClientRpc()
    {
        // 미니게임 종료이므로 특정 오브젝트 활성화 및 상태 변경
        Debug.Log("이즈미니게임플레잉False");
        isMinigamePlaying = false;
    }
    public int GetCurrentTurn()
    {
        return gameTurn.Value;
    }
    [ServerRpc(RequireOwnership = false)]
    public void DespawnNetworkObjectServerRpc(NetworkObjectReference noRef)
    {
        noRef.TryGet(out NetworkObject no);
        no.Despawn();
        Destroy(no.gameObject);
    }
    private void DestoryCharacterOnStartNode()
    {
        for (int i = PlayerManager.Instance.currentCharacters.Count - 1; i >= 0; i--)
        {
            GameObject go = PlayerManager.Instance.currentCharacters[i];
            if (go.GetComponent<CharacterBoardMovement>().currentNode == GameManager.Instance.startNode)
            {
                PlayerManager.Instance.DespawnCharacterServerRpc(go, go.GetComponent<NetworkObject>().OwnerClientId);
            }
        }
    }
}