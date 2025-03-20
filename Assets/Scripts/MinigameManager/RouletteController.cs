using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class RouletteController : NetworkBehaviour
{
    public enum BattleType
    {
        VsMatch,
        PartyTime,
        BlackHole
    }

    private int battleType;

    public Transform ContentObject;
    public GameObject RoulettePanel;
    public GameObject TypePanel;

    private List<GameObject> itemList;
    public List<GameObject> typePanelList;
    private int itemCount;
    private int rollIndex = 0;
    private int previousIndex = 0; // -1 인덱스를 의미 (직전에 플레이한 게임이 아님)
    private int previousPlayingIndex = -1; // 직전에 플레이한 게임 인덱스
    private bool isRolling = false;

    public NetworkVariable<int> MinigameIndex = new NetworkVariable<int>(-1);
    public Action EndActions;
    private Coroutine rollingCoroutine;

    private void Awake()
    {
        itemList = new List<GameObject>();

        // 자동으로 자식을 뽑아서 itemList에 삽입
        for (int i = 0; i < ContentObject.childCount; i++)
        {
            itemList.Add(ContentObject.GetChild(i).gameObject);
        }

        itemCount = itemList.Count;
        RoulettePanel.SetActive(false);
        TypePanel.SetActive(false);
    }

    public override void OnNetworkSpawn()
    {
        MinigameIndex.OnValueChanged += FixRoulette;
    }

    public void StartRoll(BattleType type)
    {
        RoulettePanel.SetActive(true);
        TypePanel.SetActive(true);
        if (IsServer)
        {
            Debug.Log($"{itemCount}개 중에서 롤렛 돌리자");
            int durationFactor = 2;
            int minRoll = itemCount * durationFactor; // 일단 최소한 돌려야되는 횟수 : 현재 14번
            int minigameIndex = UnityEngine.Random.Range(0, itemCount); // 사실상 결과를 정의하는 부분

            // 직전에 플레이한 게임은 나오지 않도록 함
            // 불필요한 딜레이를 줄이기 위해 그냥 그 다음 게임으로 지정
            if (minigameIndex == previousPlayingIndex)
            {
                minigameIndex = (minigameIndex + 1) % itemCount;
            }

            StartRollClientRpc(minRoll + minigameIndex, (int)type);
        }
    }

    [ClientRpc]
    private void StartRollClientRpc(int count, int type)
    {
        RoulettePanel.SetActive(true);
        TypePanel.SetActive(true);
        battleType = type;
        typePanelList[battleType].SetActive(true);

        Debug.Log("롤렛 돌리자 클라이언트");
        isRolling = true;
        rollingCoroutine = StartCoroutine(Rolling(count));
    }

    private void PassItemOnce()
    {
        itemList[previousIndex].SetActive(false);

        rollIndex = (rollIndex + 1) % itemCount;

        itemList[rollIndex].SetActive(true);

        previousIndex = rollIndex;
    }

    private IEnumerator Rolling(int duration)
    {
        Debug.Log("Rolling");
        int current = 0;
        float waitTime;

        while (current < duration && isRolling)
        {
            PassItemOnce();
            current++;

            waitTime = Mathf.Lerp(0.01f, 0.1f, (float)current / duration);
            AudioManager.instance.Playsfx(13);
            yield return new WaitForSecondsRealtime(waitTime);
        }

        if (IsServer)
        {
            isRolling = false;
            MinigameIndex.Value = rollIndex;
            StartCoroutine(EndRoulette());
        }
    }

    private IEnumerator EndRoulette()
    {
        GetComponent<Animator>().SetTrigger("Finish");
        AudioManager.instance.Playsfx(5);
        yield return new WaitForSecondsRealtime(2f);
        if (IsServer)
        {
            EndActions.Invoke();
        }
        typePanelList[battleType].SetActive(false);
        RoulettePanel.SetActive(false);
        TypePanel.SetActive(false);
    }

    private void FixRoulette(int oldValue, int newValue)
    {
        GetComponent<Animator>().SetTrigger("Reset");
        isRolling = false;
        StopCoroutine(rollingCoroutine);
        for (int i = 0; i < itemCount; i++)
        {
            itemList[i].SetActive(i == newValue);
        }
        StartCoroutine(EndRoulette());
    }

    public void CloseRouletteForce()
    {
        typePanelList[battleType].SetActive(false);
        RoulettePanel.SetActive(false);
        TypePanel.SetActive(false);
    }
}
