using Unity.Netcode;
using UnityEngine;

public class MainGameProgress : NetworkBehaviour
{
    int numOfPlayer;
    public int currentPlayerNumber;
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
    }
}
