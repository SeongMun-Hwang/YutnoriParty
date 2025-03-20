using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : NetworkBehaviour
{
    private static GameManager instance;
    public static GameManager Instance { get { return instance; } }

    //유니티에서 물려놓은 변수가 있으면 OnNetworkSpawn이 아닌 Awake에서 초기화해야 됨
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }
    public AnnounceCanvas announceCanvas;
    public GameObject inGameCanvas;
    public GameObject blockCanvas;
    public Image profile;
    public Image profileBackground;
    public TextMeshProUGUI playerName;
    public List<Sprite> profiles = new List<Sprite>();
    public List<Color> playerColors = new List<Color>();

    public MainGameProgress mainGameProgress;
    public PlayerBoard playerBoard;
    public Node startNode;
    public Node blackNode;
    public List<GameObject> playerCharacters;
    public List<GameObject> hideableWhenOtherScene;
    public bool isEmojiDelay = false;
    public List<Sprite> emojiList;
    public NetworkVariable<FixedString128Bytes> lobbyId = new NetworkVariable<FixedString128Bytes>();
    public NetworkVariable<FixedString128Bytes> lobbyName = new NetworkVariable<FixedString128Bytes>();
    public NetworkVariable<FixedString128Bytes> winnerName = new NetworkVariable<FixedString128Bytes>();
    public NetworkVariable<int> winnerCharacterIndex = new NetworkVariable<int>();
    public List<PlayerProfile> playerProfiles = new List<PlayerProfile>();
    public override void OnNetworkSpawn()
    {
        if(IsServer) lobbyId.Value = HostSingleton.Instance.ReturnJoinCode();
        if(IsServer) lobbyName.Value = HostSingleton.Instance.ReturnRoomName();
    }

    public void HandleEmojiDelay(float duration)
    {
        StartCoroutine(EmojiDelay(duration));
    }

    private IEnumerator EmojiDelay(float duration)
    {
        yield return new WaitForSecondsRealtime(duration);
        isEmojiDelay = false;
    }

    public void ToggleProfile()
    {
        if (playerBoard.gameObject.activeSelf)
        {
            playerBoard.gameObject.SetActive(false);
        }
        else
        {
            playerBoard.gameObject.SetActive(true);
        }
    }

    public int GetOrderOfPlayerById(ulong id)
    {
        return playerBoard.GetOrderOfPlayerById(id);
    }
}
