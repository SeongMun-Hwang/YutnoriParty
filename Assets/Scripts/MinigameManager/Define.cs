using UnityEngine;

public class Define
{
    public enum MinigameType
    {
        StackGame, // 스택게임
        ShootingGame, // 사격게임
        RunningGame, // 달리기게임
        BasketGame, // 바구니게임
        
        Randomize // 위 항목 중 랜덤으로 선택 (항상 맨 밑에 선언)
    }

    public enum MGPlayerType
    {
        Unknown, // 기본값
        Player, // 플레이어
        Spectator, // 관전자
    }
}
