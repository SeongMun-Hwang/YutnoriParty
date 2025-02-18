using Unity.Netcode;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    // �ν����Ϳ��� ������ ���� ��ġ ������Ʈ��
    [SerializeField] private Transform[] spawnPositions;

    // �÷��̾ ������ �� �ش� ��ġ�� ������Ű�� �޼���
    public Transform GetSpawnPosition(ulong clientId)
    {
        // Ŭ���̾�Ʈ ID�� �´� ��ġ�� ��ȯ
        return spawnPositions[(int)clientId % spawnPositions.Length];
    }
}