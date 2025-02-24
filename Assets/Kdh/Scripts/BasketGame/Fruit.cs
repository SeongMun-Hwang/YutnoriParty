using Unity.Netcode;
using UnityEngine;

public class Fruit : NetworkBehaviour
{
    [SerializeField] private int scoreValue; // 과일 점수
    private bool collected = false; // 중복 점수 방지

    private void OnTriggerEnter(Collider other)
    {
        if (collected) return;

        if (other.CompareTag("Basket"))
        {
            collected = true;

            NetworkObject basketObject = other.GetComponentInParent<NetworkObject>();
            if (basketObject != null)
            {
                ulong playerId = basketObject.OwnerClientId;  // 과일을 잡은 클라이언트의 ID를 가져옵니다.
                AddScoreAndDestroyServerRpc(playerId);  // 해당 플레이어에게 점수를 부여
            }
        }
        else if (other.CompareTag("Ground"))
        {
            // 땅에 닿은 경우는 서버에서 처리하도록 서버 RPC를 호출
            DestroyFruitServerRpc();  // 서버에서 과일을 제거하도록 요청
        }
    }

    // 서버에서 과일을 제거하는 ServerRpc
    [ServerRpc(RequireOwnership = false)]
    private void DestroyFruitServerRpc()
    {
        // 서버에서 과일을 삭제
        if (IsServer)
        {
            Destroy(gameObject);
        }
    }

    // 점수를 추가하고 과일을 삭제하는 ServerRpc
    [ServerRpc(RequireOwnership = false)]
    private void AddScoreAndDestroyServerRpc(ulong playerId)
    {
        BasketGameManager gameManager = FindAnyObjectByType<BasketGameManager>();

        if (gameManager != null)
        {
            gameManager.AddScore(playerId, scoreValue);  // 서버에서 해당 플레이어에게 점수를 추가
        }

        // 과일을 서버에서 삭제
        DestroyFruitServerRpc();  // 과일을 서버에서 제거
    }
}
