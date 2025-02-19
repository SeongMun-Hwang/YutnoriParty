using Unity.Netcode;
using UnityEngine;

public class MainGameProgress : NetworkBehaviour
{
    int numOfPlayer;
    public int currentPlayerNumber;
    private bool chooseCharacter = false;
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
        currentPlayerNumber = Random.Range(0, numOfPlayer);
        
        StartTurn(currentPlayerNumber);
    }
    void StartTurn(int n)
    {
        GameManager.Instance.announceCanvas.ShowAnnounceTextClientRpc(currentPlayerNumber + "'s Turn!", 2f);
        SpawnInGameCanvasClientRpc(new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { (ulong)n } } });
    }
    [ClientRpc]
    public void SpawnInGameCanvasClientRpc(ClientRpcParams clientRpcParams = default)
    {
        GameManager.Instance.inGameCanvas.SetActive(true);
        YutManager.Instance.throwChance++;
        
    }
    public void ChooseCharacter()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray=Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if(Physics.Raycast(ray, out hit))
            {
                currentCharacter=hit.collider.gameObject.GetComponent<CharacterBoardMovement>();
                Debug.Log(hit.collider.name);
            }
        }
    }
}
