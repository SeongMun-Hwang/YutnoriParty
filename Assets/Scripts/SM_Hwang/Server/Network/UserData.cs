using System;
using UnityEngine;

[SerializeField]
public enum GameMode
{
    Default
}
public enum GameQueue
{
    Solo,
    Team
}
[Serializable]
public class UserData
{
    public string userName;
    public string userAuthId;
    public GameInfo userGamePreferences = new GameInfo();
}
[Serializable]
public class GameInfo
{
    public GameMode gameMode;
    public GameQueue gameQueue;
    public string ToMultiplayQueue()
    {
        if (gameQueue == GameQueue.Team)
        {
            return "team-queue";
        }
        return "solo-queue";
    }
}