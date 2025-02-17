using Unity.Netcode;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    // 인스펙터에서 연결할 스폰 위치 오브젝트들
    [SerializeField] private Transform[] spawnPositions;

    // 플레이어가 입장할 때 해당 위치에 스폰시키는 메서드
    public Transform GetSpawnPosition(ulong clientId)
    {
        // 클라이언트 ID에 맞는 위치를 반환
        return spawnPositions[(int)clientId % spawnPositions.Length];
    }
}