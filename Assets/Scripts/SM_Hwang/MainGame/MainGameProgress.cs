using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainGameProgress : NetworkBehaviour
{
    private int numOfPlayer;
    private NetworkVariable<int> currentPlayerNumber = new NetworkVariable<int>(0);
    private bool chooseCharacter = true;
    private bool isMiniGamePlaying = false;
    public CharacterBoardMovement currentCharacter;
    private void Update()
    {
        if (chooseCharacter)
        {
            ChooseCharacter();
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
        GameManager.Instance.inGameCanvas.SetActive(true);
        YutManager.Instance.throwChance++;
    }
    /*캐릭터 선택*/
    //Ray를 통해 이동할 말 선택
    //내 말이 아니면 메시지 출력
    public void ChooseCharacter()
    {
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
                    Debug.Log("Character Choose Success");
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
        StartCoroutine(CheckOtherPlayer());
        if (YutManager.Instance.YutResultCount() == 0 && YutManager.Instance.throwChance == 0)
        {
            Debug.Log("End turn");
            GameManager.Instance.inGameCanvas.SetActive(false);
            EndTurnServerRpc();
        }
    }
    private IEnumerator CheckOtherPlayer()
    {
        Collider[] hitColliders = Physics.OverlapSphere(currentCharacter.transform.position, 2f);

        foreach (Collider collider in hitColliders)
        {
            if (collider.TryGetComponent<CharacterBoardMovement>(out var character) &&
                character.GetComponent<NetworkObject>().OwnerClientId != NetworkManager.LocalClientId)
            {
                Debug.Log("Loading StackScene...");
                if (MiniGameSceneManager.Instance == null) Debug.Log("Null");
                MiniGameSceneManager.Instance.LoadBattleScene();
                isMiniGamePlaying = true;
                break; // 한 번만 실행되도록 중단
            }
        }
        while (isMiniGamePlaying)
        {
            yield return null;
        }
    }
    /*턴 종료*/
    //다음 플레이어의 턴 시작, 이상 반복
    [ServerRpc(RequireOwnership = false)]
    void EndTurnServerRpc()
    {
        currentPlayerNumber.Value++;
        if (currentPlayerNumber.Value == numOfPlayer)
        {
            currentPlayerNumber.Value = 0;
        }
        Debug.Log("Change turn to player" + currentPlayerNumber);
        StartTurn(currentPlayerNumber.Value);
    }
}