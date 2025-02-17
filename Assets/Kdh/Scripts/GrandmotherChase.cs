using UnityEngine;
using Unity.Netcode;

public class GrandmotherChase : NetworkBehaviour
{
    [SerializeField] private float chaseSpeed = 3f;  // 할머니의 이동 속도
    [SerializeField] private Vector3 chaseDirection = Vector3.forward;  // 이동 방향
    [SerializeField] private Vector3 initialPosition; // 초기 위치

    private Vector3 grandmotherPosition;  // 할머니의 현재 위치

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
        if (!IsServer) return;  // 서버에서만 실행

        MoveGrandmother();
    }

    private void MoveGrandmother()
    {
        grandmotherPosition += chaseDirection * chaseSpeed * Time.deltaTime;
        transform.position = grandmotherPosition;

        // 모든 클라이언트에 할머니 위치를 동기화
        UpdatePositionClientRpc(grandmotherPosition);
    }

    [ClientRpc]
    private void UpdatePositionClientRpc(Vector3 newPosition)
    {
        if (IsServer) return;  // 서버는 이미 위치를 알고 있으므로 패스

        transform.position = newPosition;  // 클라이언트에서 할머니 위치 업데이트
    }

    // 플레이어와 충돌 감지 
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
        Debug.Log(player.name + "이 탈락했습니다!");
        player.gameObject.SetActive(false);
        if (IsServer)
        {
            FindAnyObjectByType<GameManager>().CheckRemainingPlayers();
        }
    }
}