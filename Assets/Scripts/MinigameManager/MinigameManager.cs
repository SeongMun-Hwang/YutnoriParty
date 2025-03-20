using System.Collections;
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

    public NetworkVariable<int> maxPlayers;
    public NetworkVariable<bool> minigameStart = new NetworkVariable<bool>(false);

    // 추후 미니게임이 추가될 때, Define 클래스의 MinigameType 열거형과 씬의 이름을 추가할 것
    private Dictionary<Define.MinigameType, string> MinigameScenes = new Dictionary<Define.MinigameType, string>()
    {
        { Define.MinigameType.StackGame, "StackScene" },
        { Define.MinigameType.ShootingGame, "ShootingScene" },
        { Define.MinigameType.RunningGame, "RunGame" },
        { Define.MinigameType.BasketGame, "BasketGame" },
        { Define.MinigameType.LuckyCalcGame, "LuckyCalcGame" },
        { Define.MinigameType.HammerGame,"HammerGame" },
        { Define.MinigameType.GrapYutGame, "GrapYutGame" }
    };
    private Define.MinigameType gameType;
    private Dictionary<ulong, Define.MGPlayerType> playerTypes;
    public List<ulong> playerList;
    public Define.MGPlayerType playerType;
    private bool isRandomGame = true;
    public bool isCheat = false;

    [SerializeField] private List<GameObject> HideableWhenMinigame;

    public Camera maingameCamera;

    [SerializeField] private GameObject devCheatMinigameMenuUI;
    [SerializeField] private GameObject spectatorUI;
    [SerializeField] private GameObject MinigameButtonUI;
    [SerializeField] private Animator FadeUIAnimator;
    [SerializeField] private RouletteController roulette;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        //if (IsServer)
        //{
        //    MinigameButtonUI.SetActive(true);
        //}
        //else
        //{
        //    MinigameButtonUI.SetActive(false);
        //}

        if (instance == null)
        {
            instance = this;
           // DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    public void StartMinigame()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            if (MainGameProgress.Instance.isGameEnd.Value) { return; }
            minigameStart.Value = true;
            if (!isRandomGame)
            {
                StartMiniGameClientRpc();
                return;
            }

            MinigameButtonUI.SetActive(false);
            roulette.gameObject.SetActive(true);

            if (!IsServer) { return; }
            roulette.EndActions = null;
            roulette.EndActions = () =>
            {
                StartMiniGameClientRpc();
                gameType = (Define.MinigameType)roulette.MinigameIndex.Value;
                Debug.Log($"랜덤으로 {gameType} 선택");
            };
            roulette.StartRoll();
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
        isRandomGame = false;
        Debug.Log($"{gameType} 미니게임 선택됨");
    }

    public void OnRandomToggleChanged()
    {
        isRandomGame = !isRandomGame;
        Debug.Log($"미니게임 랜덤 선택 : {isRandomGame}");
    }

    public void EndMinigame()
    {
        MinigameButtonUI.SetActive(true);
        EndMiniGameClientRpc();
        CloseSpectatorUIClientRpc();
    }

    [ClientRpc]
    private void UpdateSpectatorClientRpc(ulong[] players)
    {
        playerList = players.ToList();
        if (!playerList.Contains(NetworkManager.Singleton.LocalClientId))
        {
            playerType = Define.MGPlayerType.Spectator;
            spectatorUI.SetActive(true);
        }
        else
        {
            playerType = Define.MGPlayerType.Player;
        }

        Debug.Log($"상태 : {playerType}");
    }

    [ClientRpc]
    private void CloseSpectatorUIClientRpc()
    {
        spectatorUI.SetActive(false);
        playerType = Define.MGPlayerType.Unknown;
    }

    // 미니게임의 관전자와 참가자를 설정
    // 참가자의 목록만 인자로 지정해주면 나머지는 자동으로 관전자로 정해짐
    public void SetPlayers(ulong[] players)
    {
        if (NetworkManager.Singleton.IsServer && MainGameProgress.Instance.isGameEnd.Value) { return; }

        playerTypes = new Dictionary<ulong, Define.MGPlayerType>();
        maxPlayers.Value = players.Length;
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

        UpdateSpectatorClientRpc(players);
    }

    public bool IsPlayer(ulong id)
    {
        return playerTypes[id] == Define.MGPlayerType.Player;
    }

    [ClientRpc]
    void StartMiniGameClientRpc()
    {
        StartCoroutine(LoadSceneWithFade(1f, false));
    }

    [ClientRpc]
    void EndMiniGameClientRpc()
    {
        if (IsServer) { minigameStart.Value = false; }
        StartCoroutine(LoadSceneWithFade(1f, true));
    }

    private IEnumerator LoadSceneWithFade(float duration, bool isUnloading)
    {
        FadeUIAnimator.SetTrigger("FadeOut");
        yield return new WaitForSecondsRealtime(duration);
        if (IsServer)
        {
            if (!isUnloading)
            {
                NetworkManager.Singleton.SceneManager.LoadScene(MinigameScenes[gameType], LoadSceneMode.Additive);

                bool sceneLoaded = false;
                NetworkManager.Singleton.SceneManager.OnLoadComplete += (ulong clientId, string sceneName, LoadSceneMode mode) =>
                {
                    if (sceneName == MinigameScenes[gameType])
                    {
                        sceneLoaded = true;
                    }
                };

                yield return new WaitUntil(() => sceneLoaded);
            }
            else
            {
                Debug.Log("언로드 시작");
                NetworkManager.Singleton.SceneManager.UnloadScene(SceneManager.GetSceneByName(MinigameScenes[gameType]));

                bool sceneUnloaded = false;
                NetworkManager.Singleton.SceneManager.OnUnloadComplete += (ulong clientId, string sceneName) =>
                {
                    if (sceneName == MinigameScenes[gameType])
                    {
                        sceneUnloaded = true;
                    }
                };

                yield return new WaitUntil(() => sceneUnloaded);
            }
        }

        devCheatMinigameMenuUI.SetActive(isCheat && isUnloading && IsServer);
        foreach (var go in HideableWhenMinigame)
        {
            go.SetActive(isUnloading);
        }
        
        roulette.CloseRouletteForce();
        yield return new WaitForSecondsRealtime(duration);
        FadeUIAnimator.SetTrigger("FadeIn");
        yield return new WaitForSecondsRealtime(0.5f);
        if (IsServer && isUnloading && MainGameProgress.Instance.endMinigameActions != null)
        {
            MainGameProgress.Instance.endMinigameActions.Invoke();
        }
    }
}
