using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MiniGameSceneManager : NetworkBehaviour
{
    public static MiniGameSceneManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬이 변경되어도 유지
        }
        else
        {
            Destroy(gameObject);
        }
    }
    public void LoadBattleScene()
    {
        if (IsServer) // 서버에서만 씬 로드 실행
        {
            NetworkManager.SceneManager.LoadScene("StackScene", LoadSceneMode.Additive);
        }
        else
        {
            RequestSceneLoadServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestSceneLoadServerRpc(ServerRpcParams rpcParams = default)
    {
        if (IsServer)
        {
            NetworkManager.SceneManager.LoadScene("StackScene", LoadSceneMode.Additive);
        }
    }
}
