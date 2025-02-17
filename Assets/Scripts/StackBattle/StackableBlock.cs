using UnityEngine;

public class StackableBlock : MonoBehaviour
{
	// 스포너 관련
	[SerializeField] GameObject lostPrefabs;
	public BlockSpawnHandler spawner;

	// StackableBlock 이동 관련
	private float distance; // 현재 블록이 원점으로부터 떨어진 거리
	private float maxDistance; // 블록이 최대로 멀어지는 거리
	private float stepLength; // 이동 거리

	private bool moveForward; // 정면으로 이동중인가?
	public bool moveX; // X방향으로 이동중인가?

	private void Start()
	{
		maxDistance = 6f;
		distance = maxDistance;
		moveForward = false;

		if (moveX)
		{ transform.Translate(distance, 0, 0); }
		else
		{ transform.Translate(0, 0, distance); }
	}

	private void Update()
	{
		stepLength = Time.deltaTime * 6f;

		if (moveX)
		{ MoveX(); }
		else
		{ MoveZ(); }
	}

	// X방향으로 이동
	private void MoveX()
	{
		if (moveForward)
		{
			if (distance < maxDistance)
			{
				transform.Translate(stepLength, 0, 0);
				distance += stepLength;
			}
			else
			{ moveForward = false; }
		}
		else
		{
			if (distance > -maxDistance)
			{
				transform.Translate(-stepLength, 0, 0);
				distance -= stepLength;
			}
			else
			{ moveForward = true; }
		}
	}

	// Z방향으로 이동
	private void MoveZ()
	{
		if (moveForward)
		{
			if (distance < maxDistance)
			{
				transform.Translate(0, 0, stepLength);
				distance += stepLength;
			}
			else
			{ moveForward = false; }
		}
		else
		{
			if (distance > -maxDistance)
			{
				transform.Translate(0, 0, -stepLength);
				distance -= stepLength;
			}
			else
			{ moveForward = true; }
		}
	}

	public void ScaleBlock()
	{

	}
}
