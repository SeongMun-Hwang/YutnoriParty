using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MinigameManager : NetworkBehaviour
{
    private static MinigameManager instance;
    public static MinigameManager Instance { get { return instance; } }

    private Dictionary<Define.MinigameType, string> MinigameScenes = new Dictionary<Define.MinigameType, string>()
    {
        { Define.MinigameType.StackGame, "StackScene" },
        { Define.MinigameType.ShootingGame, "ShootingScene" },
        { Define.MinigameType.RunningGame, "RunGame" },
    };

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    // 미니게임을 시작하기 위해 씬을 이동
    public void StartMinigame(Define.MinigameType type)
    {
        if (NetworkManager.Singleton.IsServer)
        {
            if (MinigameScenes.TryGetValue(type, out string sceneName))
            {
                NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
            }
            else
            {
                Debug.LogError($"씬 {type}이 없음");
            }
        }
        else
        {
            Debug.LogWarning("서버가 아니라 씬 변경 못함");
        }
    }

    // 씬에서 버튼에 직접 연결하기 위해 임시로 만든 메서드
    // 추후 코드상에서는 위의 enum 매개변수 타입을 사용할 것
    public void StartMinigame(int i)
    {
        StartMinigame((Define.MinigameType)i);
    }

    public void EndMinigame()
    {
        MainGameProgress.Instance.endMinigameActions.Invoke();
    }
}
