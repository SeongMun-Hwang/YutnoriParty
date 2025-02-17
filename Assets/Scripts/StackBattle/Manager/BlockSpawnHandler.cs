using System.Collections.Generic;
using UnityEngine;

public class BlockSpawnHandler : MonoBehaviour
{
	// 게임 엔티티 관련
	[SerializeField] private GameObject blockPrefab;
	[SerializeField] private GameObject bottomFrame;
	private List<GameObject> stack; // 현재까지 쌓인 스택

	// 색상 관련
	[SerializeField] private List<Color32> teamColors;
	private int colorIndex;

	private void Start()
	{
		stack = new List<GameObject>();
		colorIndex = 0;
		stack.Add(bottomFrame);
		stack[0].GetComponent<Renderer>().material.color = teamColors[0];
		CreateBlock();
	}

	public void DropBlock()
	{
		if (stack.Count > 1)
		{ stack[stack.Count - 1].GetComponent<StackableBlock>().ScaleBlock(); }

		CreateBlock();
	}

	public void CreateBlock()
	{
		GameObject previousTile = stack[stack.Count - 1];
		GameObject activeTile;

		activeTile = Instantiate(blockPrefab);
		stack.Add(activeTile);

		if (stack.Count > 2)
			activeTile.transform.localScale = previousTile.transform.localScale;

		activeTile.transform.position = new Vector3(previousTile.transform.position.x,
			previousTile.transform.position.y + previousTile.transform.localScale.y, previousTile.transform.position.z);

		colorIndex++;
		if (colorIndex >= teamColors.Count)
		{ colorIndex = 0; }

		activeTile.GetComponent<Renderer>().material.color = teamColors[colorIndex];
		activeTile.GetComponent<StackableBlock>().spawner = this;
		activeTile.GetComponent<StackableBlock>().moveX = stack.Count % 2 == 0;
	}
}
