using System.Collections.Generic;
using System.Resources;
using Unity.Netcode;
using UnityEngine;

public class MinigameManager : MonoBehaviour
{
    public enum MinigameType
    {
        StackGame, // 스택게임
        ShootingGame, // 사격게임
        RunningGame, // 달리기게임
        //CatchingGame, // 바구니게임
    }

    public Dictionary<MinigameType, string> MinigameScenes = new Dictionary<MinigameType, string>()
    {
        { MinigameType.StackGame, "StackScene" },
        { MinigameType.ShootingGame, "ShootingScene" },
        { MinigameType.RunningGame, "RunGame" },
        //{ MinigameType.CatchingGame, "BasketGame" }
    };

    // 미니게임을 시작하기 위해 씬을 이동
    public void StartMinigame(MinigameType type)
    {
        string sceneName = MinigameScenes[type];
        NetworkManager.Singleton.SceneManager.LoadScene(sceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
    }

    // 씬에서 버튼에 직접 연결하기 위해 임시로 만든 메서드
    // 추후 코드상에서는 위의 enum 매개변수 타입을 사용
    public void StartMinigame(int i)
    {
        StartMinigame((MinigameType)i);
    }
}
