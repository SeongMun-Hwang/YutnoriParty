using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ShootingBattleManager : NetworkBehaviour
{
    // 게임에 참여하는 유저 관련
    private NetworkList<ulong> playerIds = new NetworkList<ulong>(); // 참가한 플레이어 ID 리스트
    int currentId = -1;

    // UI 관련
    [SerializeField] private TMP_Text timerUI;
    [SerializeField] private List<TMP_Text> usernameUI;
    [SerializeField] private List<TMP_Text> scoreUI;
    [SerializeField] private GameObject winMessageUI;
    [SerializeField] private GameObject loseMessageUI;
    [SerializeField] private List<Color32> crosshairColors;

    // 게임 상태 및 진행 관련
    [SerializeField] public NetworkVariable<bool> isPlaying;
    private bool gameStart = false;
    [SerializeField] private float timer = 30f;
    private NetworkList<int> playerScore = new NetworkList<int>(); // 플레이어들의 획득 점수
    private int topPlayerId = 0;
    private int topPlayerScore = 0;

    // 게임 규칙 관련
    [SerializeField] private float spawnDuration = 1.5f;

    // 게임 요소 관련
    [SerializeField] Camera mainCamera;
    [SerializeField] GameObject StarPrefab;
    [SerializeField] GameObject CursorPrefab;

    int colorIndex = 0;
    public override void OnNetworkSpawn()
    {
        isPlaying.OnValueChanged += InitScoreBoardUI;
        if (IsServer)
        {
            foreach (var clientPair in NetworkManager.Singleton.ConnectedClients)
            {
                ulong clientId = clientPair.Key;
                OnPlayerJoined(clientId);
            }
        }

        currentId = playerIds.IndexOf(NetworkManager.Singleton.LocalClientId);
        Debug.Log($"플레이어 ID : {currentId}");
    }

    private void OnPlayerJoined(ulong clientId)
    {
        if (!playerIds.Contains(clientId) && MinigameManager.Instance.IsPlayer(clientId))
        {
            playerIds.Add(clientId);
            playerScore.Add(0);

            GameObject cursor = Instantiate(CursorPrefab);
            NetworkCrosshair nc = cursor.GetComponent<NetworkCrosshair>();

            Scene minigameScene = SceneManager.GetSceneByName("ShootingScene");
            SceneManager.MoveGameObjectToScene(cursor, minigameScene);

            NetworkObject cursorNetObj = cursor.GetComponent<NetworkObject>();
            cursorNetObj.SpawnWithOwnership(clientId, true);

            nc.networkColor.Value = crosshairColors[colorIndex++];

            if (playerIds.Count == MinigameManager.Instance.maxPlayers.Value)
            {
                StartCoroutine(StartGameTimer(5));
            }
        }
    }

    private IEnumerator StartGameTimer(int timer = 3)
    {
        while (timer > 0)
        {
            yield return new WaitForSecondsRealtime(1f);
            timer--;
            GameManager.Instance.announceCanvas.ShowAnnounceTextClientRpc(timer.ToString(), 0.7f, Color.white);
            yield return null;
        }

        GameManager.Instance.announceCanvas.ShowAnnounceTextClientRpc("Start!", 1f, Color.white);

        isPlaying.Value = true;
    }

    public void InitScoreBoardUI(bool previousValue, bool newValue)
    {
        for (int i = 0; i < playerIds.Count; i++)
        {
            usernameUI[i].transform.parent.gameObject.SetActive(true);
            foreach (PlayerProfileData data in GameManager.Instance.playerBoard.playerProfileDatas)
            {
                if (data.clientId == playerIds[i])
                {
                    usernameUI[i].text = data.userName.ToString();
                    usernameUI[i].color = crosshairColors[i];
                }
            }
            
        }
    }

    private void Update()
    {
        if (isPlaying.Value)
        {
            if (!gameStart)
            {
                StartCoroutine(SpawnStar());
                StartCoroutine(CountTimer());
                gameStart = true;
            }
            else
            {
                UpdateScoreUI();
            }
        }

        if (Input.GetMouseButtonDown(0) && isPlaying.Value) // 마우스 왼쪽 클릭
        {
            if (MinigameManager.Instance.playerType != Define.MGPlayerType.Player) { Debug.Log("관전중"); return; }

            Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mousePosition, Vector2.zero);

            if (hit.collider != null)
            {
                // 클릭된 오브젝트의 특정 스크립트 실행
                ShootableStar star = hit.collider.GetComponent<ShootableStar>();
                if (star != null)
                {
                    star.OnClick(crosshairColors[currentId]);
                    AddScoreServerRpc(currentId);
                }
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void AddScoreServerRpc(int id)
    {
        playerScore[id]++;

        // 가장 높은 점수를 가장 빠르게 달성한 사람을 기록
        if (topPlayerScore < playerScore[id])
        {
            topPlayerScore = playerScore[id];
            topPlayerId = id;
            //Debug.Log($"현재 1등 {topPlayerId} : {topPlayerScore}");
        }
    }

    private void UpdateScoreUI()
    {
        // Debug.Log($"UI 변경 {playerScore[0]} {playerScore[1]}");
        for (int i = 0; i < MinigameManager.Instance.maxPlayers.Value; i++)
        {
            scoreUI[i].text = playerScore[i].ToString();
        }
    }

    private IEnumerator CountTimer()
    {
        while (isPlaying.Value)
        {
            yield return null;
            timer--;
            timerUI.text = timer.ToString();
            yield return new WaitForSecondsRealtime(1f);

            if (timer == 0)
            {
                if (IsServer)
                {
                    isPlaying.Value = false;
                    Debug.Log($"게임 종료! 플레이어 {playerIds[topPlayerId]} 승리");

                    MainGameProgress.Instance.winnerId = playerIds[topPlayerId];
                    GameFinishedClientRpc(playerIds[topPlayerId]);
                    StartCoroutine(PassTheScene());
                }
                yield break;
            }
        }
    }

    private IEnumerator SpawnStar()
    {
        if (!IsServer)
        {
            yield break;
        }

        while (isPlaying.Value)
        {
            yield return null;

            Vector3 randomPos = new Vector3(Random.Range(-7f, 7f), Random.Range(-2f, 2f), 0);
            GameObject star = Instantiate(StarPrefab, randomPos, transform.rotation);
            star.GetComponent<ShootableStar>().manager = this;
            star.GetComponent<NetworkObject>().Spawn(true);

            yield return new WaitForSecondsRealtime(spawnDuration);
            spawnDuration = Mathf.Clamp(spawnDuration - 0.4f, 0.2f, 1.5f);
        }
    }

    [ClientRpc]
    public void GameFinishedClientRpc(ulong winClientId)
    {
        if (MinigameManager.Instance.playerType != Define.MGPlayerType.Player) { return; }

        if (NetworkManager.Singleton.LocalClientId == winClientId)
        {
            winMessageUI.SetActive(true);
        }
        else
        {
            loseMessageUI.SetActive(true);
        }

        Debug.Log("게임 종료");
    }

    public IEnumerator PassTheScene()
    {
        yield return new WaitForSecondsRealtime(2f);
        MinigameManager.Instance.EndMinigame();
    }
}
