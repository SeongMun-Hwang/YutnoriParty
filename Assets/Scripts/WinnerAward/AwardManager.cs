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
        // TODO : 각종 싱글톤 및 게임 데이터를 삭제하고 타이틀로 돌아가기
        if (NetworkManager.Singleton.IsHost)
        {
            HostSingleton.Instance.ShutDown();
        }
        if (NetworkManager.Singleton.IsConnectedClient)
        {
            NetworkManager.Singleton.Shutdown();
        }
        if (FindFirstObjectByType<HostSingleton>() != null)
            Destroy(FindFirstObjectByType<HostSingleton>().gameObject);

        if (FindFirstObjectByType<ClientSingleton>() != null)
            Destroy(FindFirstObjectByType<ClientSingleton>().gameObject);

        if (FindFirstObjectByType<ServerSingleton>() != null)
            Destroy(FindFirstObjectByType<ServerSingleton>().gameObject);
        SceneManager.LoadScene("MenuScene");
    }
}
