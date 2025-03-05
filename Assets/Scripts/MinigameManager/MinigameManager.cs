using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MinigameManager : NetworkBehaviour
{
    private static MinigameManager instance;
    public static MinigameManager Instance { get { return instance; } }

    public int maxPlayers;
    private Dictionary<Define.MinigameType, string> MinigameScenes = new Dictionary<Define.MinigameType, string>()
    {
        { Define.MinigameType.StackGame, "StackScene" },
        { Define.MinigameType.ShootingGame, "ShootingScene" },
        { Define.MinigameType.RunningGame, "RunGame" },
    };
    private Define.MinigameType gameType;
    private Dictionary<ulong, Define.MGPlayerType> playerTypes;

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
                Debug.LogError($"씬 {type}이 없습니다");
            }
        }
        else
        {
            Debug.LogWarning("씬 변경은 서버에서 수행해야합니다");
        }
    }

    public void StartMinigame()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            NetworkManager.Singleton.SceneManager.LoadScene(MinigameScenes[gameType], LoadSceneMode.Additive);
        }
        else
        {
            Debug.LogWarning("씬 변경은 서버에서 수행해야합니다");
        }
    }

    // 씬에서 버튼에 직접 연결하기 위해 임시로 만든 메서드
    // 추후 코드상에서는 위의 enum 매개변수 타입을 사용할 것
    public void SelectMinigame(int i)
    {
        gameType = (Define.MinigameType)i;
    }

    public void EndMinigame()
    {
        MainGameProgress.Instance.endMinigameActions.Invoke();
    }

    // 미니게임의 관전자와 참가자를 설정
    // 참가자의 목록만 인자로 지정해주면 나머지는 자동으로 관전자로 정해짐
    public void SetPlayers(ulong[] players)
    {
        playerTypes = new Dictionary<ulong, Define.MGPlayerType>();
        maxPlayers = players.Length;
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (players.Contains(clientId))
            {
                playerTypes.Add(clientId, Define.MGPlayerType.Player);
                Debug.Log($"{clientId} 참가자");
            }
            else
            {
                playerTypes.Add(clientId, Define.MGPlayerType.Spectator);
                Debug.Log($"{clientId} 관전자");
            }
        }
    }

    public bool IsPlayer(ulong id)
    {
        return playerTypes[id] == Define.MGPlayerType.Player;
    }
}
