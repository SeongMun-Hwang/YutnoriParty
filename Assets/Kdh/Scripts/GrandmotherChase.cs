using UnityEngine;
using Unity.Netcode;

public class GrandmotherChase : NetworkBehaviour
{
    [SerializeField] private float chaseSpeed = 3f;  // �ҸӴ��� �̵� �ӵ�
    [SerializeField] private Vector3 chaseDirection = Vector3.forward;  // �̵� ����
    [SerializeField] private Vector3 initialPosition; // �ʱ� ��ġ

    private Vector3 grandmotherPosition;  // �ҸӴ��� ���� ��ġ

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer) // ���������� �ʱ� ��ġ ����
        {
            grandmotherPosition = initialPosition;
            transform.position = grandmotherPosition;
        }
    }

    private void Update()
    {
        if (!IsServer) return;  // ���������� ����

        MoveGrandmother();
    }

    private void MoveGrandmother()
    {
        grandmotherPosition += chaseDirection * chaseSpeed * Time.deltaTime;
        transform.position = grandmotherPosition;

        // ��� Ŭ���̾�Ʈ�� �ҸӴ� ��ġ�� ����ȭ
        UpdatePositionClientRpc(grandmotherPosition);
    }

    [ClientRpc]
    private void UpdatePositionClientRpc(Vector3 newPosition)
    {
        if (IsServer) return;  // ������ �̹� ��ġ�� �˰� �����Ƿ� �н�

        transform.position = newPosition;  // Ŭ���̾�Ʈ���� �ҸӴ� ��ġ ������Ʈ
    }

    // �÷��̾�� �浹 ���� 
    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return; 

        if (other.TryGetComponent<PlayerController>(out PlayerController player))
        {
            PlayerEliminated(player);
        }
    }

    private void PlayerEliminated(PlayerController player)
    {
        Debug.Log(player.name + "�� Ż���߽��ϴ�!");
        player.gameObject.SetActive(false);
        if (IsServer)
        {
            FindAnyObjectByType<GameManager>().CheckRemainingPlayers();
        }
    }
}