using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class FruitSpawner : NetworkBehaviour
{
    [SerializeField] private GameObject[] fruitPrefabs; 
    [SerializeField] private float spawnInterval = 2f; 
    [SerializeField] private float spawnRangeX = 10f; 
    [SerializeField] private float spawnRangeZ = 10f; 
    [SerializeField] private float spawnHeight = 10f;
    private bool isSpawning = false;

    private void Update()
    {
        if (IsServer && BasketGameManager.Instance.gameStart && !isSpawning)
        {
            isSpawning = true;
            StartCoroutine(SpawnFruits());
        }
    }

        

    private IEnumerator SpawnFruits()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);

            // FruitSpawner가 위치한 위치를 기준으로 랜덤한 x, z 좌표 계산
            float randomX = Random.Range(-spawnRangeX, spawnRangeX);
            float randomZ = Random.Range(-spawnRangeZ, spawnRangeZ);
            Vector3 spawnPosition = new Vector3(transform.position.x + randomX, spawnHeight, transform.position.z + randomZ);
           

            // 여러 과일 중 하나를 랜덤하게 선택
            int randomIndex = Random.Range(0, fruitPrefabs.Length);

            // 선택된 과일을 서버로 전송 (프리팹의 인덱스 전달)
            SpawnFruitServerRpc(spawnPosition, randomIndex);
        }
    }

    // 서버에서 과일을 인스턴스화하고 네트워크 상에 생성하도록 하는 ServerRpc
    [ServerRpc]
    private void SpawnFruitServerRpc(Vector3 position, int fruitIndex)
    {
        

        // 과일 프리팹을 서버에서 선택하여 인스턴스화하고 네트워크에서 생성
        GameObject selectedFruit = fruitPrefabs[fruitIndex];
        GameObject fruit = Instantiate(selectedFruit, position, Quaternion.identity);

        // 네트워크 객체 추가
        NetworkObject networkObject = fruit.GetComponent<NetworkObject>();
        if (networkObject != null)
        {
          
            networkObject.Spawn();  // 네트워크에서 객체를 스폰
        }
        else
        {
          
        }
    }
}
