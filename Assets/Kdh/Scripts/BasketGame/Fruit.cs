using UnityEngine;
using Unity.Netcode;

public class Fruit : NetworkBehaviour
{
    [SerializeField] private int scoreValue; 

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Basket")) // 바구니와 충돌하면
        {
            if (other.TryGetComponent(out BasketScore playerScore))
            {
                playerScore.AddScore(scoreValue); 
            }

            // 서버에서 과일 삭제
            if (IsServer)
            {
                DestroyFruit();
            }
            else
            {
                // 클라이언트에서 서버에게 과일 삭제 요청
                DestroyFruitServerRpc();
            }
        }

        if (other.CompareTag("Ground")) // 바닥에 닿으면
        {
            // 서버에서 과일 삭제
            if (IsServer)
            {
                DestroyFruit();
            }
            else
            {
                // 클라이언트에서 서버에게 과일 삭제 요청
                DestroyFruitServerRpc();
            }
        }
    }

  
    private void DestroyFruit()
    {
        NetworkObject networkObject = GetComponent<NetworkObject>();
        if (networkObject != null)
        {
            networkObject.Despawn(); // 네트워크에서 객체 삭제
        }
        else
        {
            Debug.LogError("No NetworkObject found on the fruit!");
        }
    }


    [ServerRpc]
    private void DestroyFruitServerRpc()
    {
        DestroyFruit(); // 서버에서 삭제
    }
}
