using NUnit.Framework;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AwardManager : MonoBehaviour
{
    [SerializeField] TMP_Text winnerName;
    [SerializeField] Transform winnerSpawnPoint;
    [SerializeField] List<GameObject> characterPrefabs;

    private void Start()
    {
        AudioManager.instance.Playsfx(12);
        GameObject winner = Instantiate(characterPrefabs[GameManager.Instance.winnerCharacterIndex.Value]);
        SceneManager.MoveGameObjectToScene(winner, SceneManager.GetSceneByName("AwardScene"));
        winner.transform.SetParent(winnerSpawnPoint, false);
        winnerName.text = GameManager.Instance.winnerName.Value.ToString();
    }

    public void BackToLobby()
    {
        if (NetworkManager.Singleton.IsHost)
        {
            CleanupNetworkObjects();
            HostSingleton.Instance.ShutDown();
        }

        if (NetworkManager.Singleton.IsConnectedClient)
        {
            NetworkManager.Singleton.Shutdown();
        }

        SceneManager.LoadScene("MenuScene");
    }

    public void CleanupNetworkObjects()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            List<NetworkObject> spawnedObjects = new List<NetworkObject>(NetworkManager.Singleton.SpawnManager.SpawnedObjects.Values);

            foreach (var networkObject in spawnedObjects)
            {
                if (networkObject.IsSpawned)
                {
                    networkObject.Despawn();
                    Destroy(networkObject.gameObject);
                }
            }
        }
    }
}
