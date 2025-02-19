using UnityEngine;
using Unity.Netcode;

public class GrandmotherChase : NetworkBehaviour
{
    [SerializeField] private float chaseSpeed = 3f;  // 할머니의 이동 속도
    [SerializeField] private Vector3 chaseDirection = Vector3.forward;  // 이동 방향
    [SerializeField] private Vector3 initialPosition; // 초기 위치
    private bool canChase = false;
    private Vector3 grandmotherPosition;  // 할머니의 현재 위치
    private Animator animator;

    private void Start()
    {
        animator = GetComponent<Animator>();
    }
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer) // 서버에서만 초기 위치 설정
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
            animator.SetFloat("MoveSpeed", 0f); // Idle 상태 유지
        }
    }
    private void MoveGrandmother()
    {
        grandmotherPosition += chaseDirection * chaseSpeed * Time.deltaTime;
        transform.position = grandmotherPosition;
        float speedValue = chaseSpeed > 0 ? 1f : 0f; // 속도에 따라 애니메이션 변경
        animator.SetFloat("MoveSpeed", speedValue);
        // 모든 클라이언트에 할머니 위치를 동기화
        UpdatePositionClientRpc(grandmotherPosition, speedValue);
    }

    [ClientRpc]
    private void UpdatePositionClientRpc(Vector3 newPosition, float speedValue)
    {
        if (IsServer) return;  // 서버는 이미 위치를 알고 있으므로 패스

        transform.position = newPosition;  // 클라이언트에서 할머니 위치 업데이트
        animator.SetFloat("MoveSpeed", speedValue);
    }

    // 플레이어와 충돌 감지 
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
        Debug.Log(player.name + "이 탈락했습니다!");
        player.SetEliminated(true);
        FindAnyObjectByType<GrandMaGameManager>().CheckRemainingPlayers();
    }
    public void EnableChase()
    {
        canChase = true;
    }
}