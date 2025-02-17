using System.Globalization;
using Unity.Netcode;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] private float moveDistance = 2f;  // 한 번 이동할 거리
    [SerializeField] private float moveSpeed = 5f;     // 이동 속도

    private Vector3 targetPosition;  // 목표 위치

    private void Start()
    {
        targetPosition = transform.position;  // 초기 목표 위치를 현재 위치로 설정
    }

    private void Update()
    {
        if (!IsOwner) return; // 네트워크에서 본인만 조작 가능

        // 스페이스바를 누르면 앞으로 이동
        if (Input.GetKeyDown(KeyCode.Space))
        {
            MoveForwardServerRpc();
        }

        // 부드럽게 이동
        transform.position = Vector3.Lerp(transform.position, targetPosition, moveSpeed * Time.deltaTime);
    }

    [ServerRpc]
    private void MoveForwardServerRpc()
    {
        targetPosition += transform.forward * moveDistance;
        MoveClientRpc(targetPosition);  // 모든 클라이언트에 적용
    }

    [ClientRpc]
    private void MoveClientRpc(Vector3 newPosition)
    {
        targetPosition = newPosition;
    }
}
