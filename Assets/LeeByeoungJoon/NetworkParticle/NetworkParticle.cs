using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class NetworkParticle : NetworkBehaviour
{
    [SerializeField] ParticleSystem particle;
    NetworkObject networkObject;
    float duration;

    private void Awake()
    {
        duration = particle.main.duration;
        networkObject = GetComponent<NetworkObject>();
        networkObject.Spawn();
    }

    public override void OnNetworkSpawn()
    {
        particle.gameObject.SetActive(true); //파티클 재생
        DespawnSelf();
    }

    //파티클 지속시간 후에 스스로 디스폰
    IEnumerator DespawnSelf()
    {
        yield return new WaitForSeconds(duration);

        networkObject.Despawn();
        Destroy(gameObject);
    }
}
