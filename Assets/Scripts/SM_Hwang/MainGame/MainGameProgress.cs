using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class MainGameProgress : NetworkBehaviour
{
    private int numOfPlayer;
    private NetworkVariable<int> currentPlayerNumber = new NetworkVariable<int>(0);
    private NetworkVariable<int> gameTurn=new NetworkVariable<int>(0);
    public CharacterBoardMovement currentCharacter;
    private GameObject encounteredEnemy;
    public Camera maingameCamera;
    private static MainGameProgress instance;
    public static MainGameProgress Instance { get { return instance; } }
    public System.Action endMinigameActions;
    public ulong winnerId;
    private void Update()
    {
        ChooseCharacter();
    }
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }
    /*게임 시작*/
    //시작 시 입장한 플레이어 수 저장 및 랜덤으로 시작턴 지정
    public void StartGame()
    {
        numOfPlayer = NetworkManager.ConnectedClients.Count;
        currentPlayerNumber.Value = Random.Range(0, numOfPlayer);
        YutManager.Instance.HideYutRpc(); //윷 안보이게 함
        StartTurn(currentPlayerNumber.Value);
    }
    /*턴 시작*/
    //누구의 턴인지 공지
    void StartTurn(int n)
    {
        GameManager.Instance.announceCanvas.ShowAnnounceTextClientRpc(currentPlayerNumber.Value + "'s Turn!", 2f);
        SpawnInGameCanvasClientRpc(new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { (ulong)n } } });
    }
    /*UI 소환*/
    //현재 턴인 클라이언트에 게임 진행을 위한 캔버스 액티브, 던지기 기회++
    [ClientRpc]
    public void SpawnInGameCanvasClientRpc(ClientRpcParams clientRpcParams = default)
    {
        StartCoroutine(WaitForCanvasAndActivate());
    }

    private IEnumerator WaitForCanvasAndActivate()
    {
        while (GameManager.Instance.inGameCanvas == null)
        {
            Debug.LogWarning("Waiting for inGameCanvas...");
            yield return null;
        }
        GameManager.Instance.inGameCanvas.SetActive(true);
        YutManager.Instance.throwChance++;
    }
    /*캐릭터 선택*/
    //Ray를 통해 이동할 말 선택
    //내 말이 아니면 메시지 출력
    public void ChooseCharacter()
    {
        if ((int)NetworkManager.LocalClientId != currentPlayerNumber.Value) return; //내 턴이 아니면 작동 X
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                if (hit.collider.TryGetComponent<NetworkObject>(out var networkObject) &&
                    networkObject.OwnerClientId != NetworkManager.LocalClientId)
                {
                    GameManager.Instance.announceCanvas.ShowAnnounceText("Not your player", 2f);
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
    [ServerRpc(RequireOwnership = false)]
    void StartMiniGameServerRpc(NetworkObjectReference playerReference, NetworkObjectReference enemyReference)
    {
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

        // 미니 게임이 끝났을 때 서버에서 발생시킬 이벤트를 지정
        endMinigameActions = null;
        endMinigameActions += (() =>
        {
            EndMiniGameClientRpc();

            //미니 게임 승자 판별과 패배한 말 처리
            GameManager.Instance.announceCanvas.ShowAnnounceTextClientRpc("Player" + winnerId + "Win!", 2f);
            if (winnerId == playerNetObj.OwnerClientId)
            {
                Debug.Log("Attacker Win / Enemy Lose");
                AddThrowChanceClientRpc(winnerId);
                //YutManager.Instance.throwChance++;
                //enemy.GetComponent<CharacterInfo>().DespawnServerRpc();
                PlayerManager.Instance.DespawnCharacterServerRpc(enemy, enemy.GetComponent<NetworkObject>().OwnerClientId);
            }
            else
            {
                Debug.Log("Attacker Lose / Enemy Win");
                //currentCharacter.GetComponent<CharacterInfo>().DespawnServerRpc();
                PlayerManager.Instance.DespawnCharacterServerRpc(player, player.GetComponent<NetworkObject>().OwnerClientId);
            }
        });
        ulong[] players = new ulong[2] { playerNetObj.OwnerClientId, enemyNetObj.OwnerClientId };
        MinigameManager.Instance.SetPlayers(players);
        MinigameManager.Instance.StartMinigame();
        StartMiniGameClientRpc();
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
    void StartMiniGameClientRpc()
    {
        // 미니게임을 위해 특정 오브젝트 비활성화
        maingameCamera.gameObject.SetActive(false); // 윷놀이 판 전용카메라
        YutManager.Instance.gameObject.SetActive(false); // 윷놀이 관련 비활성화
    }

    [ClientRpc]
    void EndMiniGameClientRpc()
    {
        // 미니게임 종료이므로 특정 오브젝트 활성화 및 상태 변경
        isMinigamePlaying = false;
        maingameCamera.gameObject.SetActive(true); // 윷놀이 판 전용카메라
        YutManager.Instance.gameObject.SetActive(true); // 윷놀이 관련 활성화
    }
    public int GetCurrentTurn()
    {
        return gameTurn.Value;
    }
}