using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class HammerGameManager : NetworkBehaviour
{
    [SerializeField] List<GameObject> hammerCharacterPrefabs;
    [SerializeField] List<Transform> spawnPos;
    [SerializeField] Camera watchCamera;
    [SerializeField] public Transform lookAtTransform;
    [SerializeField] GameObject pillar;
    [SerializeField] GameObject guidePanel;
    [SerializeField] PlayerUICanvas playerUICanvas;
    private static HammerGameManager instance;
    private List<GameObject> playingCharacters = new List<GameObject>();
    private int playerNum = 0;
    private float timer = 45f;
    public static HammerGameManager Instance
    {
        get { return instance; }
    }
    private void Awake()
    {
        instance = this;
    }
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            foreach (ulong clientId in NetworkManager.ConnectedClientsIds)
            {
                if (MinigameManager.Instance.IsPlayer(clientId))
                {
                    playerNum++;
                    SpawnHammerCharacterServerRpc(clientId);
                }
                else
                {
                    ChangeToWatchCameraClientRpc(clientId);
                }
            }
        }
    }
    [ServerRpc(RequireOwnership = false)]
    private void SpawnHammerCharacterServerRpc(ulong clientId)
    {
        int index = PlayerManager.Instance.GetClientIndex(clientId);
        GameObject go = Instantiate(hammerCharacterPrefabs[index], spawnPos[index]);
        go.GetComponent<NetworkObject>().SpawnWithOwnership(clientId);
        playingCharacters.Add(go);
        if (playingCharacters.Count == playerNum)
        {
            StartCoroutine(StartGameTimer(5));
        }
    }
    [ServerRpc(RequireOwnership = false)]
    public void AddForceWithHammerServerRpc(NetworkObjectReference noRef, Vector3 forceDir)
    {
        if (noRef.TryGet(out NetworkObject no))
        {
            ulong targetClientId = no.OwnerClientId; // 대상 클라이언트 ID 가져오기
            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { targetClientId } }
            };

            AddForceWithHammerClientRpc(noRef, forceDir, clientRpcParams);
        }
    }

    [ClientRpc]
    private void AddForceWithHammerClientRpc(NetworkObjectReference noRef, Vector3 forceDir, ClientRpcParams clientRpcParams = default)
    {
        Debug.Log("add force client rpc");
        if (noRef.TryGet(out NetworkObject no))
        {
            if (!no.IsOwner) return;

            Rigidbody rb = no.gameObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(forceDir, ForceMode.Impulse);
            }
        }
    }
    [ServerRpc(RequireOwnership = default)]
    public void DespawnLoserServerRpc(NetworkObjectReference noRef)
    {
        noRef.TryGet(out NetworkObject no);
        if (playingCharacters.Count != 1)
        {
            playerUICanvas.SetPlayerDeadClientRpc(no.OwnerClientId);
        }
        ChangeToWatchCameraClientRpc(no.OwnerClientId);
        no.Despawn();
        playingCharacters.Remove(no.gameObject);
        Destroy(no.gameObject);
        CheckHammerGameEnd();

    }
    [ClientRpc]
    private void ChangeToWatchCameraClientRpc(ulong targetId)
    {
        if (NetworkManager.LocalClientId != targetId) return;
        watchCamera.gameObject.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        watchCamera = Camera.main;
    }
    private void CheckHammerGameEnd()
    {
        if (playingCharacters.Count == 1)
        {
            Debug.Log("Hammer Game End");
            MainGameProgress.Instance.winnerId = playingCharacters[0].GetComponent<NetworkObject>().OwnerClientId;
            DespawnLoserServerRpc(playingCharacters[0]);
            Cursor.lockState = CursorLockMode.None;
            MinigameManager.Instance.EndMinigame();
        }
    }
    private IEnumerator StartGameTimer(int timer = 3)
    {
        while (timer > 0)
        {
            yield return new WaitForSecondsRealtime(1f);
            timer--;
            if (timer == 0) break;
            GameManager.Instance.announceCanvas.ShowAnnounceTextClientRpc(timer.ToString(), 0.7f);
            yield return null;
        }
        guidePanel.SetActive(false);
        foreach (GameObject player in playingCharacters)
        {
            player.GetComponent<HammerGameController>().StartHammerGameClientRpc();
        }
        StartCoroutine(PillarScaleDecrease());

    }
    private IEnumerator PillarScaleDecrease()
    {
        Vector3 initialScale = pillar.transform.localScale; // 초기 크기 저장
        while (timer > 0)
        {
            timer -= 0.1f;
            yield return new WaitForSecondsRealtime(0.1f);
            if (timer <= 15f)
            {
                float scaleFactor = Mathf.Max(timer / 15f, 0.3f);
                pillar.transform.localScale = new Vector3(initialScale.x * scaleFactor, initialScale.y, initialScale.z * scaleFactor);
            }
        }
        yield break;
    }
}