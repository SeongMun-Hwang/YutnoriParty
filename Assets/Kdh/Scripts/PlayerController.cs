using Unity.Netcode;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] private float moveDistance = 2f;  // �� �� �̵��� �Ÿ�
    [SerializeField] private float moveSpeed = 5f;     // �̵� �ӵ�

    private Vector3 targetPosition;

    private void Start()
    {
        Transform spawnTransform = FindFirstObjectByType<SpawnManager>().GetSpawnPosition(OwnerClientId);
        targetPosition = spawnTransform.position;
        
    }

    private void Update()
    {
        if (!IsOwner) return; // ���θ� ���� �����ϵ��� ����
        Camera.main.transform.position = transform.position + new Vector3(0, 5, 5);
        if (Input.GetKeyDown(KeyCode.Space))
        {
            MoveForwardServerRpc(OwnerClientId);  
        }

        transform.position = Vector3.Lerp(transform.position, targetPosition, moveSpeed * Time.deltaTime);
    }

    [ServerRpc]
    private void MoveForwardServerRpc(ulong clientId)
    {
        // �ش� Ŭ���̾�Ʈ�� ��ġ ����
        if (OwnerClientId != clientId) return;

        targetPosition += transform.forward * moveDistance;
        MoveClientRpc(targetPosition);
    }

    [ClientRpc]
    private void MoveClientRpc(Vector3 newPosition)
    {
        targetPosition = newPosition;
    }
}
