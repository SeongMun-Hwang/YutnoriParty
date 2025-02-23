using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class ShootingBattleManager : NetworkBehaviour
{
    // 게임에 참여하는 유저 관련
    [SerializeField] private int maxPlayers;
    private NetworkList<ulong> playerIds = new NetworkList<ulong>(); // 참가한 플레이어 ID 리스트

    // UI 관련
    [SerializeField] private TMP_Text timerUI;
    [SerializeField] private List<TMP_Text> scoreUI;

    // 게임 상태 및 진행 관련
    [SerializeField] private NetworkVariable<bool> isPlaying;
    private bool gameStart = false;
    [SerializeField] private float timer = 15f;
    private NetworkList<int> playerScore = new NetworkList<int>(); // 플레이어들의 획득 점수

    // 게임 규칙 관련
    [SerializeField] private float spawnDuration = 1.5f;

    // 게임 요소 관련
    [SerializeField] GameObject StarPrefab;

    private void Start()
    {
        
    }

    public override void OnNetworkSpawn()
    {
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

            if (playerIds.Count == maxPlayers)
            {
                isPlaying.Value = true;
            }
        }
    }

    private void Update()
    {
        if (isPlaying.Value && !gameStart)
        {
            StartCoroutine(SpawnStar());
            StartCoroutine(CountTimer());
            gameStart = true;
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
                }
            }
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
                isPlaying.Value = false;
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
            Instantiate(StarPrefab, randomPos, transform.rotation);

            yield return new WaitForSecondsRealtime(spawnDuration);
            spawnDuration = Mathf.Clamp(spawnDuration - 0.4f, 0.2f, 1.5f);
        }
    }
}
