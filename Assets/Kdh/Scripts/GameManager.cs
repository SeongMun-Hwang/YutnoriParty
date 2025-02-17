using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class GameManager : NetworkBehaviour
{
    [SerializeField] private Collider goalArea;  // 골인 지점의 콜라이더

    private void Update()
    {
        if (IsServer)
        {
            if (NetworkManager.Singleton.ConnectedClients.Count >= 2)
            {

                CheckForWinner();  // 골인 지점에 도달한 플레이어 확인
                CheckRemainingPlayers();
            }
        }
    }

    private void CheckForWinner()
    {
        // 네트워크 상의 모든 플레이어를 확인하여 골인 지점에 도달했는지 확인
        foreach (var client in NetworkManager.Singleton.ConnectedClients)
        {
            var playerObject = client.Value.PlayerObject;

            if (playerObject.TryGetComponent(out PlayerController playerController))
            {
                // 골인 지점에 도달했는지 확인
                if (goalArea.bounds.Contains(playerController.transform.position))
                {
                    EndGame(playerController);  // 골인한 플레이어가 있으면 게임 종료
                    return;
                }
            }
        }
    }
    public void CheckRemainingPlayers()
    {
        List<PlayerController> alivePlayers = new List<PlayerController>();

        foreach (var client in NetworkManager.Singleton.ConnectedClients)
        {
            var playerObject = client.Value.PlayerObject;

            if (playerObject.TryGetComponent(out PlayerController playerController) && playerController.gameObject.activeSelf)
            {
                alivePlayers.Add(playerController);
            }
        }

        if (alivePlayers.Count == 1)
        {
            EndGame(alivePlayers[0]);  // 마지막 남은 플레이어가 승리
        }
    }

    private void EndGame(PlayerController winner)
    {
        Debug.Log(winner.name + "이 승리했습니다!");

        // 승자가 있으면 나머지 플레이어 탈락 처리
        foreach (var client in NetworkManager.Singleton.ConnectedClients)
        {
            var playerObject = client.Value.PlayerObject;

            if (playerObject.TryGetComponent(out PlayerController playerController) && playerController != winner)
            {
                // PlayerController 대신 NetworkObject의 NetworkObjectId를 전달
                EndGame_ClientRpc(playerController.NetworkObject.NetworkObjectId);
            }
        }

        Debug.Log("미니게임이 종료되었습니다.");
    }



    [ClientRpc]
    private void EndGame_ClientRpc(ulong networkObjectId)
    {
        // NetworkObjectId를 사용하여 PlayerController를 찾은 후 비활성화
        var playerObject = NetworkManager.Singleton.SpawnManager.SpawnedObjects[networkObjectId];

        if (playerObject != null && playerObject.TryGetComponent(out PlayerController playerController))
        {
            playerController.gameObject.SetActive(false);  // 클라이언트에서도 플레이어 비활성화
        }
    }
}
