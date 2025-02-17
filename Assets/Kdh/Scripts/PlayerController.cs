using System.Globalization;
using Unity.Netcode;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] private float moveDistance = 2f;  // �� �� �̵��� �Ÿ�
    [SerializeField] private float moveSpeed = 5f;     // �̵� �ӵ�

    private Vector3 targetPosition;  // ��ǥ ��ġ

    private void Start()
    {
        targetPosition = transform.position;  // �ʱ� ��ǥ ��ġ�� ���� ��ġ�� ����
    }

    private void Update()
    {
        if (!IsOwner) return; // ��Ʈ��ũ���� ���θ� ���� ����

        // �����̽��ٸ� ������ ������ �̵�
        if (Input.GetKeyDown(KeyCode.Space))
        {
            MoveForwardServerRpc();
        }

        // �ε巴�� �̵�
        transform.position = Vector3.Lerp(transform.position, targetPosition, moveSpeed * Time.deltaTime);
    }

    [ServerRpc]
    private void MoveForwardServerRpc()
    {
        targetPosition += transform.forward * moveDistance;
        MoveClientRpc(targetPosition);  // ��� Ŭ���̾�Ʈ�� ����
    }

    [ClientRpc]
    private void MoveClientRpc(Vector3 newPosition)
    {
        targetPosition = newPosition;
    }
}
