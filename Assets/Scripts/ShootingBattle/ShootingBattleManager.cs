using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

public class ShootingBattleManager : NetworkBehaviour
{
    // 게임에 참여하는 유저 관련
    [SerializeField] private int maxPlayers;
    private NetworkList<ulong> playerIds = new NetworkList<ulong>(); // 참가한 플레이어 ID 리스트
    int currentId = -1;

    // UI 관련
    [SerializeField] private TMP_Text timerUI;
    [SerializeField] private List<TMP_Text> scoreUI;

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
    [SerializeField] GameObject StarPrefab;

    private void Start()
    {
        
    }

    public override void OnNetworkSpawn()
    {
        currentId = playerIds.Count;
        Debug.Log($"플레이어 ID : {currentId}");
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnPlayerJoined;
        }
    }

    private void OnPlayerJoined(ulong clientId)
    {
        if (!playerIds.Contains(clientId))
        {
            playerIds.Add(clientId);
            playerScore.Add(0);

            if (playerIds.Count == maxPlayers)
            {
                isPlaying.Value = true;
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

        if (Input.GetMouseButtonDown(0)) // 마우스 왼쪽 클릭
        {
            Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mousePosition, Vector2.zero);

            if (hit.collider != null)
            {
                // 클릭된 오브젝트의 특정 스크립트 실행
                ShootableStar star = hit.collider.GetComponent<ShootableStar>();
                if (star != null)
                {
                    star.OnClick();
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
            Debug.Log($"현재 1등 {topPlayerId} : {topPlayerScore}");
        }
    }

    private void UpdateScoreUI()
    {
        for (int i = 0; i < maxPlayers; i++)
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
                }
                yield break;
            }
        }
    }

    private IEnumerator SpawnStar()
    {
        while (isPlaying.Value)
        {
            yield return null;

            Vector3 randomPos = new Vector3(Random.Range(-7f, 7f), Random.Range(-2f, 2f), 0);
            GameObject star = Instantiate(StarPrefab, randomPos, transform.rotation);
            star.GetComponent<ShootableStar>().manager = this;

            yield return new WaitForSecondsRealtime(spawnDuration);
            spawnDuration = Mathf.Clamp(spawnDuration - 0.4f, 0.2f, 1.5f);
        }
    }
}
