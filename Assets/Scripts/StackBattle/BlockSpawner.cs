using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BlockSpawner : MonoBehaviour
{
    // 게임 상태 관련
    public enum BattleState { Ready, Start, End }
    public BattleState state;

    // UI 관련
    [SerializeField] private GameObject startButton;
	[SerializeField] private TMP_Text scoreText;

	// 필드 오브젝트 관련
	[SerializeField] private GameObject blockPrefabs;
	[SerializeField] private GameObject bottomBlock;
	private Stack<GameObject> blockStack;

	// 블록 색상 관련
	private List<Color32> spectrum = new List<Color32>(){
		new Color32(0, 255, 33, 255)    ,
		new Color32(167, 255, 0, 255)   ,
		new Color32(230, 255, 0, 255)   ,
		new Color32(255, 237, 0, 255)   ,
		new Color32(255, 206, 0, 255)   ,
		new Color32(255, 185, 0, 255)   ,
		new Color32(255, 142, 0, 255)   ,
		new Color32(255, 111, 0, 255)   ,
		new Color32(255, 58, 0, 255)    ,
		new Color32(255, 0, 0, 255)     ,
		new Color32(255, 0, 121, 255)   ,
		new Color32(255, 0, 164, 255)   ,
		new Color32(241, 0, 255, 255)   ,
		new Color32(209, 0, 255, 255)   ,
		new Color32(178, 0, 255, 255)   };
	private int modifier;
	private int colorIndex;

	private void Start()
	{
		scoreText.text = "0";
		blockStack = new Stack<GameObject>();
		state = BattleState.Start;
		modifier = 1;
		colorIndex = 0;

		blockStack.Push(bottomBlock);
		blockStack.Peek().GetComponent<Renderer>().material.color = spectrum[0];

		CreateBlock();
	}

	private void Update()
	{
		switch(state)
		{
			case BattleState.Ready:

				break;
			case BattleState.Start:
				UpdateStart();
				break;
			case BattleState.End:

				break;
		}
	}

	private void UpdateStart()
	{
		if (Input.GetMouseButtonDown(0))
		{
			if (blockStack.Count > 1)
			{
				blockStack.Peek().GetComponent<StackableBlock>().ScaleTile();
			}

			scoreText.text = (blockStack.Count - 1).ToString();
			CreateBlock();
		}
	}

	private void CreateBlock()
	{
		GameObject previousBlock = blockStack.Peek();
		GameObject newBlockObject = Instantiate(blockPrefabs);
		blockStack.Push(newBlockObject);

		StackableBlock newBlockScript = newBlockObject.GetComponent<StackableBlock>();
		newBlockScript.spawner = this;

		if (blockStack.Count > 2)
		{ newBlockObject.transform.localScale = previousBlock.transform.localScale; }

		newBlockObject.transform.position = previousBlock.transform.position + Vector3.up;

		colorIndex += modifier;
		if (colorIndex == spectrum.Count || colorIndex == -1)
		{
			modifier *= -1;
			colorIndex += 2 * modifier;
		}

		newBlockObject.GetComponent<Renderer>().material.color = spectrum[colorIndex];
		newBlockScript.GetComponent<StackableBlock>().moveX = blockStack.Count % 2 == 0;
	}

	public void GameOver()
	{
		state = BattleState.End;
	}
}
