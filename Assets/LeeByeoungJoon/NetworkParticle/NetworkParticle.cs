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
        //InitParticleRpc();
    }
    public override void OnNetworkSpawn()
    {
        PlayParticleRpc();
        DespawnSelf();
    }

    [Rpc(SendTo.Server)]
    void InitParticleRpc()
    {
        duration = particle.main.duration;
        networkObject = GetComponent<NetworkObject>();
        networkObject.Spawn();
    }

    [Rpc(SendTo.ClientsAndHost)]
    void PlayParticleRpc()
    {
        particle.gameObject.SetActive(true); //파티클 재생
    }

    //파티클 지속시간 후에 스스로 디스폰
    IEnumerator DespawnSelf()
    {
        yield return new WaitForSeconds(duration);

        networkObject.Despawn();
        Destroy(gameObject);
    }
}
