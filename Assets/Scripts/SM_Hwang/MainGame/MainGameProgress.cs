using Unity.Netcode;
using UnityEngine;

public class MainGameProgress : NetworkBehaviour
{
    int numOfPlayer;
    private NetworkVariable<int> currentPlayerNumber = new NetworkVariable<int>(0);
    private bool chooseCharacter = true;
    public CharacterBoardMovement currentCharacter;
    private void Update()
    {
        if (chooseCharacter)
        {
            ChooseCharacter();
        }
    }
    public void StartGame()
    {
        numOfPlayer = NetworkManager.ConnectedClients.Count;
        currentPlayerNumber.Value = Random.Range(0, numOfPlayer);

        StartTurn(currentPlayerNumber.Value);
    }
    void StartTurn(int n)
    {
        GameManager.Instance.announceCanvas.ShowAnnounceTextClientRpc(currentPlayerNumber.Value + "'s Turn!", 2f);
        SpawnInGameCanvasClientRpc(new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { (ulong)n } } });
    }
    [ClientRpc]
    public void SpawnInGameCanvasClientRpc(ClientRpcParams clientRpcParams = default)
    {
        GameManager.Instance.inGameCanvas.SetActive(true);
        YutManager.Instance.throwChance++;
        //EndTurn();
    }
    public void ChooseCharacter()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                if (hit.collider.gameObject.TryGetComponent<CharacterBoardMovement>(out var character))
                {
                    Debug.Log("Character Choose Success");
                    currentCharacter = character;
                }
            }
        }
    }
    public void EndMove()
    {
        //윷 리스트 없으면 턴 종료
        Debug.Log("End turn");
        GameManager.Instance.inGameCanvas.SetActive(false);
        EndTurnServerRpc();
    }
    [ServerRpc(RequireOwnership = false)]
    void EndTurnServerRpc()
    {
        if (!IsServer) return;
        currentPlayerNumber.Value++;
        if (currentPlayerNumber.Value == numOfPlayer)
        {
            currentPlayerNumber.Value = 0;
        }
        Debug.Log("Change turn to player"+currentPlayerNumber);
        StartTurn(currentPlayerNumber.Value);
    }
}