using UnityEngine;
using Unity.Netcode;

public class Chase : NetworkBehaviour
{
    [SerializeField] private float chaseSpeed = 3f;  
    [SerializeField] private Vector3 chaseDirection = Vector3.forward;  
    [SerializeField] private Vector3 initialPosition; 
    private bool canChase = false;
    private Vector3 chaserPosition; 
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
            chaserPosition = initialPosition;
            transform.position = chaserPosition;
        }
    }

    private void Update()
    {
        if (!IsServer) return;

        if (canChase)
        {
            MoveChaser();
        }
        else
        {
            animator.SetFloat("MoveSpeed", 0f); // Idle 상태 유지
        }
    }
    private void MoveChaser()
    {
        chaserPosition += chaseDirection * chaseSpeed * Time.deltaTime;
        transform.position = chaserPosition;
        float speedValue = chaseSpeed > 0 ? 1f : 0f; // 속도에 따라 애니메이션 변경
        animator.SetFloat("MoveSpeed", speedValue);        
        // 모든 클라이언트에 위치를 동기화
        UpdatePositionClientRpc(chaserPosition, speedValue);
    }
    [ClientRpc]
    private void PlayMoveSoundClientRpc()
    {
        AudioManager.instance.Playsfx(0);
    }
    [ClientRpc]
    private void PlayCatchSoundClientRpc()
    {
        AudioManager.instance.Playsfx(1);
    }
    [ClientRpc]
    private void UpdatePositionClientRpc(Vector3 newPosition, float speedValue)
    {
        if (IsServer) return;  // 서버는 이미 위치를 알고 있으므로 패스

        transform.position = newPosition;  // 클라이언트에서  위치 업데이트
        animator.SetFloat("MoveSpeed", speedValue);
    }

    // 플레이어와 충돌 감지 
    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;
        
        if (other.TryGetComponent<RunGameController>(out RunGameController player))
        {
            player.EnableControl(false);
            Debug.Log(player.name + "이 탈락했습니다!");
            animator.SetTrigger("hit");
            FindAnyObjectByType<RunGameManager>().CheckRemainingPlayers();
        }
    }

   
    public void EnableChase()
    {
        canChase = true;
    }
}