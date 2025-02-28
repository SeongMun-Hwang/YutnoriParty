using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainGameProgress : NetworkBehaviour
{
    private int numOfPlayer;
    private NetworkVariable<int> currentPlayerNumber = new NetworkVariable<int>(0);
    public CharacterBoardMovement currentCharacter;
    private GameObject encounteredEnemy;
    private static MainGameProgress instance;
    public static MainGameProgress Instance { get { return instance; } }
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
        if ((int)NetworkManager.LocalClientId != currentPlayerNumber.Value
            || PlayerManager.Instance.isMoving) return; //내 턴이 아니면 작동 X
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
    /*말 이동*/
    //말 선택이 되어있으면 선택된 말의 이동 함수 실행
    public void MoveCurrentCharacter(int n)
    {
        if (currentCharacter == null)
        {
            GameManager.Instance.announceCanvas.ShowAnnounceText("Choose Character First!", 2f);
            return;
        }
        currentCharacter.MoveToNextNode(n);
    }
    /*이동 종료 함수*/
    //말이 윷 결과에 따른 이동을 마쳤을 때마다 호출
    //더 이상 던질 기회와 이동 가능한 결과가 없으면 턴 종료
    public void EndMove()
    {
        Debug.Log("End Move");
        if (CheckOtherPlayer())
        {
            StartMiniGame(encounteredEnemy);
        }
        CheckTurnChange();

    }
    private bool CheckOtherPlayer()
    {
        Collider[] hitColliders = Physics.OverlapSphere(currentCharacter.transform.position, 2f);
        foreach (Collider collider in hitColliders)
        {
            if (collider.gameObject == currentCharacter.gameObject) continue;
            if (collider.TryGetComponent<CharacterBoardMovement>(out var character))
            {
                if (character.GetComponent<NetworkObject>().OwnerClientId != (ulong)currentPlayerNumber.Value) //적이면
                {
                    encounteredEnemy = character.gameObject;
                    return true;
                }
                else if (character.GetComponent<NetworkObject>().OwnerClientId == (ulong)currentPlayerNumber.Value)//내 말이면
                {
                    PlayerManager.Instance.OverlapCharacter(character.gameObject, currentCharacter.gameObject);
                    currentCharacter.GetComponent<Outline>().DisableOutline();
                    currentCharacter=character;
                    character.GetComponent<Outline>().EnableOutline();
                    return false;
                }
            }
        }
        return false;
    }
    private void CheckTurnChange()
    {
        Debug.Log("Check Turn Change");
        if (YutManager.Instance.YutResultCount() == 0 && YutManager.Instance.throwChance == 0)
        {
            GameManager.Instance.inGameCanvas.SetActive(false);
            EndTurnServerRpc();
        }
    }
    private void StartMiniGame(GameObject enemy)
    {
        //미니게임 실행

        //미니게임 종료

        //미니 게임 승자 아이디 받기
        ulong winnerId = (ulong)Random.Range(0, 2);
        GameManager.Instance.announceCanvas.ShowAnnounceTextClientRpc("Player" + winnerId + "Win!", 2f);
        if (winnerId == NetworkManager.LocalClientId)
        {
            Debug.Log("You Win");
            YutManager.Instance.throwChance++;
            //enemy.GetComponent<CharacterInfo>().DespawnServerRpc();
            PlayerManager.Instance.DespawnCharacterServerRpc(enemy, enemy.GetComponent<NetworkObject>().OwnerClientId);
        }
        else
        {
            Debug.Log("You Lose");
            //currentCharacter.GetComponent<CharacterInfo>().DespawnServerRpc();
            PlayerManager.Instance.DespawnCharacterServerRpc(currentCharacter.gameObject, currentCharacter.GetComponent<NetworkObject>().OwnerClientId);
        }
    }
    /*턴 종료*/
    //다음 플레이어의 턴 시작, 이상 반복
    [ServerRpc(RequireOwnership = false)]
    void EndTurnServerRpc()
    {
        Debug.Log("Change Turn");
        currentPlayerNumber.Value++;
        if (currentPlayerNumber.Value == numOfPlayer)
        {
            currentPlayerNumber.Value = 0;
        }
        StartTurn(currentPlayerNumber.Value);
    }
}