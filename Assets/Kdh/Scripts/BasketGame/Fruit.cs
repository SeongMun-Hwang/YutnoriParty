using Unity.Netcode;
using UnityEngine;
using UnityEngine.Audio;

public class Fruit : NetworkBehaviour
{
    [SerializeField] private int scoreValue; 
    [SerializeField] private GameObject collectParticlePrefab;
    private bool collected = false;
    private AudioSource audioSource;
    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }
        private void OnTriggerEnter(Collider other)
        {
        if (collected) return;

      

        if (other.CompareTag("Basket"))
        {
            collected = true;

            // 부모에 있는 NetworkObject 찾기 (플레이어에 존재)
            NetworkObject basketObject = other.GetComponentInParent<NetworkObject>();
            if (basketObject == null)
            {
               
                return;
            }

            ulong playerId = basketObject.OwnerClientId;

            
            BasketGameController controller = other.GetComponentInParent<BasketGameController>();

            if (controller == null)
            {
                controller = other.transform.root.GetComponent<BasketGameController>(); // 최상위 부모에서 찾기
            }

            if (controller == null)
            {
              
                return;
            }
            else
            {
               
            }
            controller.AddScore(scoreValue);
            SpawnParticleServerRpc(transform.position);
            PlaySoundClientRpc();
            Invoke(nameof(DestroyFruitServerRpc), 0.2f);//인보크 안하니깐 소리재생전에 파괴
        }
        else if (other.CompareTag("Ground"))
        {
          
            DestroyFruitServerRpc();
        }
    }
    [ClientRpc]
    private void PlaySoundClientRpc()
    {
        if (audioSource != null)
        {
            audioSource.Play();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void DestroyFruitServerRpc()
    {
        if (IsServer)
        {
            Destroy(gameObject);  // 서버에서 과일 삭제
        }
    }
    [ServerRpc(RequireOwnership = false)]
    private void SpawnParticleServerRpc(Vector3 position)
    {
        if (collectParticlePrefab != null)
        {
            GameObject particleInstance = Instantiate(collectParticlePrefab, position, Quaternion.identity);
            NetworkObject networkParticle = particleInstance.GetComponent<NetworkObject>();

            if (networkParticle != null)
            {
                networkParticle.SpawnWithOwnership(0, true); // 서버에서 생성 후 모든 클라이언트에 동기화
                Destroy(networkParticle.gameObject, 0.75f);
            }
        }
    }

    
}
