using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    private static GameManager instance;
    public static GameManager Instance { get { return instance; } }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }
    public AnnounceCanvas announceCanvas;
    public GameObject inGameCanvas;
    public MainGameProgress mainGameProgress;
    public Node startNode;
    public List<GameObject> playerCharacters;
}
