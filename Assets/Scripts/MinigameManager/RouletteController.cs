using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class RouletteController : NetworkBehaviour
{
    public Transform ContentObject;
    public GameObject RoulettePanel;

    private List<GameObject> itemList;
    private int itemCount;
    private int rollIndex = 0;
    private int previousIndex = 0;
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
    }

    public override void OnNetworkSpawn()
    {
        MinigameIndex.OnValueChanged += FixRoulette;
    }

    public void StartRoll()
    {
        RoulettePanel.SetActive(true);
        if (IsServer)
        {
            Debug.Log($"{itemCount}개 중에서 롤렛 돌리자");
            int durationFactor = 5;
            int minRoll = itemCount * durationFactor;
            int minigameIndex = UnityEngine.Random.Range(0, itemCount);
            StartRollClientRpc(minRoll + minigameIndex);
        }
    }

    [ClientRpc]
    private void StartRollClientRpc(int count)
    {
        RoulettePanel.SetActive(true);
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
        float waitTime = 0.05f;

        while (current < duration && isRolling)
        {
            PassItemOnce();
            current++;

            waitTime = Mathf.Lerp(0.01f, 0.4f, (float)current / duration);
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
        yield return new WaitForSecondsRealtime(2f);
        if (IsServer)
        {
            EndActions.Invoke();
        }
        RoulettePanel.SetActive(false);
    }

    private void FixRoulette(int oldValue, int newValue)
    {
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
        RoulettePanel.SetActive(false);
    }
}
