using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BattleSceneManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (GameObject.FindFirstObjectByType<NetworkManager>() == null)
        {
            SceneManager.LoadScene("NetConnect");
            return;
        }
    }

    public void Disconnect()
    {
        if (NetworkManager.Singleton.IsHost)
        {
            HostSingleton.Instance.ShutDown();
        }
        if (NetworkManager.Singleton.IsConnectedClient)
        {
            NetworkManager.Singleton.Shutdown();
        }
    }
}
