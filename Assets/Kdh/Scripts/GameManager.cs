using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class GameManager : NetworkBehaviour
{
    [SerializeField] private Collider goalArea;  // ���� ������ �ݶ��̴�

    private void Update()
    {
        if (IsServer)
        {
            if (NetworkManager.Singleton.ConnectedClients.Count >= 2)
            {

                CheckForWinner();  // ���� ������ ������ �÷��̾� Ȯ��
                CheckRemainingPlayers();
            }
        }
    }

    private void CheckForWinner()
    {
        // ��Ʈ��ũ ���� ��� �÷��̾ Ȯ���Ͽ� ���� ������ �����ߴ��� Ȯ��
        foreach (var client in NetworkManager.Singleton.ConnectedClients)
        {
            var playerObject = client.Value.PlayerObject;

            if (playerObject.TryGetComponent(out PlayerController playerController))
            {
                // ���� ������ �����ߴ��� Ȯ��
                if (goalArea.bounds.Contains(playerController.transform.position))
                {
                    EndGame(playerController);  // ������ �÷��̾ ������ ���� ����
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
            EndGame(alivePlayers[0]);  // ������ ���� �÷��̾ �¸�
        }
    }

    private void EndGame(PlayerController winner)
    {
        Debug.Log(winner.name + "�� �¸��߽��ϴ�!");

        // ���ڰ� ������ ������ �÷��̾� Ż�� ó��
        foreach (var client in NetworkManager.Singleton.ConnectedClients)
        {
            var playerObject = client.Value.PlayerObject;

            if (playerObject.TryGetComponent(out PlayerController playerController) && playerController != winner)
            {
                // PlayerController ��� NetworkObject�� NetworkObjectId�� ����
                EndGame_ClientRpc(playerController.NetworkObject.NetworkObjectId);
            }
        }

        Debug.Log("�̴ϰ����� ����Ǿ����ϴ�.");
    }



    [ClientRpc]
    private void EndGame_ClientRpc(ulong networkObjectId)
    {
        // NetworkObjectId�� ����Ͽ� PlayerController�� ã�� �� ��Ȱ��ȭ
        var playerObject = NetworkManager.Singleton.SpawnManager.SpawnedObjects[networkObjectId];

        if (playerObject != null && playerObject.TryGetComponent(out PlayerController playerController))
        {
            playerController.gameObject.SetActive(false);  // Ŭ���̾�Ʈ������ �÷��̾� ��Ȱ��ȭ
        }
    }
}
