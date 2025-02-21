using UnityEngine;
using Unity.Netcode;

public class BasketScore : NetworkBehaviour
{
    private int score = 0;

    public void AddScore(int value)
    {
        if (!IsServer) return; 
        score += value;
        UpdateScoreClientRpc(score);
    }

    [ClientRpc]
    private void UpdateScoreClientRpc(int newScore)
    {
        // 점수 UI 업데이트
    }

    public int GetScore() => score;
}