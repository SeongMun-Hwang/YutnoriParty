using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BlockSpawnHandler : NetworkBehaviour
{
	// 게임 엔티티 관련
	public StackBattleManager manager;
	[SerializeField] private GameObject blockPrefab;
	[SerializeField] private GameObject bottomFrame;
	[SerializeField] private List<GameObject> stack;
    [SerializeField] private float blockSpeed = 6f;
    private bool isSpawned = false;
    Scene stackScene;

    public override void OnNetworkSpawn()
	{
		if (IsServer)
		{
            Debug.Log("이거");

            // 기존 블록들을 Despawn() 후 리스트 비우기
            foreach (var block in stack)
            {
                if (block != null && block.TryGetComponent(out NetworkObject netObj) && netObj.IsSpawned)
                {
                    netObj.Despawn();
                }
            }

            stack.Clear();
        }
	}

    private void Update()
    {
        if (stack.Count > 0 && !isSpawned)
        {
            StackableBlock previousTile = stack[stack.Count - 1].GetComponent<StackableBlock>();
            if (IsServer && manager.isPlaying.Value && !previousTile.isMoving)
            {
                isSpawned = true;
                CreateBlock();
            }
        }
            
    }

    public void CreateBlock()
	{
		if (!IsServer || !manager.isPlaying.Value)
			return; // 서버에서만 블록 생성

		GameObject previousTile;
		GameObject activeTile;

		if (stack.Count > 0)
		{
			previousTile = stack[stack.Count - 1];

            if (previousTile.GetComponent<StackableBlock>().isMoving)
            {
                return;
            }
		}
		else
		{
			previousTile = bottomFrame;
		}

		activeTile = Instantiate(blockPrefab);
		stack.Add(activeTile);
		activeTile.transform.localScale = previousTile.transform.localScale;
		activeTile.transform.position = previousTile.transform.position + Vector3.up;
		activeTile.GetComponent<StackableBlock>().moveX = Random.Range(0, 2) == 0;
		activeTile.GetComponent<StackableBlock>().spawner = this;
		activeTile.GetComponent<StackableBlock>().manager = manager;
		activeTile.GetComponent<StackableBlock>().moveSpeed = blockSpeed;
        if (stack.Count % MinigameManager.Instance.maxPlayers.Value == 0)
        {
            blockSpeed += 0.25f;
        }
        Scene minigameScene = SceneManager.GetSceneByName("StackScene");
        SceneManager.MoveGameObjectToScene(activeTile, minigameScene);

        NetworkObject netObj = activeTile.GetComponent<NetworkObject>();
		netObj.Spawn(true); // 네트워크에 생성 등록
	}

	public void DropBlock()
	{
		if (!IsServer) return; // 서버에서만 블록 배치
        isSpawned = false;
		stack[stack.Count - 1].GetComponent<StackableBlock>().ScaleBlock();
	}
}