using UnityEngine;
using Unity.Netcode;

public class GrandmotherChase : NetworkBehaviour
{
    [SerializeField] private float chaseSpeed = 3f;  // �ҸӴ��� �̵� �ӵ�
    [SerializeField] private Vector3 chaseDirection = Vector3.forward;  // �̵� ����
    [SerializeField] private Vector3 initialPosition; // �ʱ� ��ġ
    private bool canChase = false;
    private Vector3 grandmotherPosition;  // �ҸӴ��� ���� ��ġ
    private Animator animator;

    private void Start()
    {
        animator = GetComponent<Animator>();
    }
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
        if (!IsServer) return;

        if (canChase)
        {
            MoveGrandmother();
        }
        else
        {
            animator.SetFloat("MoveSpeed", 0f); // Idle ���� ����
        }
    }
    private void MoveGrandmother()
    {
        grandmotherPosition += chaseDirection * chaseSpeed * Time.deltaTime;
        transform.position = grandmotherPosition;
        float speedValue = chaseSpeed > 0 ? 1f : 0f; // �ӵ��� ���� �ִϸ��̼� ����
        animator.SetFloat("MoveSpeed", speedValue);
        // ��� Ŭ���̾�Ʈ�� �ҸӴ� ��ġ�� ����ȭ
        UpdatePositionClientRpc(grandmotherPosition, speedValue);
    }

    [ClientRpc]
    private void UpdatePositionClientRpc(Vector3 newPosition, float speedValue)
    {
        if (IsServer) return;  // ������ �̹� ��ġ�� �˰� �����Ƿ� �н�

        transform.position = newPosition;  // Ŭ���̾�Ʈ���� �ҸӴ� ��ġ ������Ʈ
        animator.SetFloat("MoveSpeed", speedValue);
    }

    // �÷��̾�� �浹 ���� 
    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;
        
        if (other.TryGetComponent<PlayerController>(out PlayerController player))
        {
            PlayerEliminated(player);
            animator.SetTrigger("hit");
        }
    }

    private void PlayerEliminated(PlayerController player)
    {
        Debug.Log(player.name + "�� Ż���߽��ϴ�!");
        player.SetEliminated(true);
        FindAnyObjectByType<GrandMaGameManager>().CheckRemainingPlayers();
    }
    public void EnableChase()
    {
        canChase = true;
    }
}