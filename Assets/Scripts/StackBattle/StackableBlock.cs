using UnityEngine;

public class StackableBlock : MonoBehaviour
{
	// StackableBlock 이동 관련
	private float distance; // 현재 블록이 원점으로부터 떨어진 거리
	private float maxDistance; // 블록이 최대로 멀어지는 거리
	private float stepTime; // 이동 시간

	private bool moveForward; // 정면으로 이동중인가?
	private bool moveX; // X방향으로 이동중인가?

	private void Start()
	{
		maxDistance = 7f;
		distance = maxDistance;
		moveForward = false;

		if (moveX)
		{ transform.Translate(distance, 0, 0); }
		else
		{ transform.Translate(0, 0, distance); }
	}

	private void Update()
	{
		stepTime = Time.deltaTime * 6f;

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
				transform.Translate(stepTime, 0, 0);
				distance += stepTime;
			}
			else
			{ moveForward = false; }
		}
		else
		{
			if (distance > -maxDistance)
			{
				transform.Translate(-stepTime, 0, 0);
				distance -= stepTime;
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
				transform.Translate(0, 0, stepTime);
				distance += stepTime;
			}
			else
			{ moveForward = false; }
		}
		else
		{
			if (distance > -maxDistance)
			{
				transform.Translate(0, 0, -stepTime);
				distance -= stepTime;
			}
			else
			{ moveForward = true; }
		}
	}
}
