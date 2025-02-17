using Unity.Netcode;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] private float moveDistance = 2f;  // 한 번 이동할 거리
    [SerializeField] private float moveSpeed = 5f;     // 이동 속도

    private Vector3 targetPosition;

    private void Start()
    {
        Transform spawnTransform = FindFirstObjectByType<SpawnManager>().GetSpawnPosition(OwnerClientId);
        targetPosition = spawnTransform.position;
        
    }

    private void Update()
    {
        if (!IsOwner) return; // 본인만 조작 가능하도록 설정
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
        // 해당 클라이언트만 위치 변경
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
