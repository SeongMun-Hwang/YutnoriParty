using UnityEngine;

public class StackableBlock : MonoBehaviour
{
	// 스포너 관련
	[SerializeField] GameObject lostPrefabs;
	public BlockSpawner spawner;

	// StackableBlock 이동 관련
	private float distance; // 현재 블록이 원점으로부터 떨어진 거리
	private float maxDistance; // 블록이 최대로 멀어지는 거리
	private float stepTime; // 이동 시간

	private bool moveForward; // 정면으로 이동중인가?
	public bool moveX; // X방향으로 이동중인가?

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

	public void ScaleTile()
	{
		if (Mathf.Abs(distance) > 0.2f)
		{
			float lostLength = Mathf.Abs(distance);

			if (moveX)
			{ 
				if (transform.localScale.x < lostLength)
				{
					gameObject.AddComponent<Rigidbody>();
					spawner.GameOver();
					return;
				}

				GameObject lostPiece = Instantiate(lostPrefabs);
				lostPiece.transform.localScale = new Vector3(lostLength, transform.localScale.y, transform.localScale.z);
				lostPiece.transform.position = new Vector3(transform.position.x
					+ (distance > 0 ? 1 : -1) * (transform.localScale.x - lostLength) / 2,
					transform.position.y, transform.position.z);
				lostPiece.GetComponent<Renderer>().material.SetColor("_Color",
					GetComponent<Renderer>().material.GetColor("_Color"));

				transform.localScale -= new Vector3(lostLength, 0, 0);
				transform.Translate((distance > 0 ? -1 : 1) * lostLength / 2, 0, 0);
			}
			else
			{
				if (transform.localScale.z < lostLength)
				{
					gameObject.AddComponent<Rigidbody>();
					spawner.GameOver();
					return;
				}

				GameObject lostPiece = Instantiate(lostPrefabs);
				lostPiece.transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y, lostLength);
				lostPiece.transform.position = new Vector3(transform.position.x,
					transform.position.y, transform.position.z
					+ (distance > 0 ? 1 : -1) * (transform.localScale.x - lostLength) / 2);
				lostPiece.GetComponent<Renderer>().material.SetColor("_Color",
					GetComponent<Renderer>().material.GetColor("_Color"));

				transform.localScale -= new Vector3(0, 0, lostLength);
				transform.Translate(0, 0, (distance > 0 ? -1 : 1) * lostLength / 2);
			}

		}
		else
		{
			if (moveX)
			{ transform.Translate(-distance, 0, 0); }
			else
			{ transform.Translate(0, 0, -distance); }
		}

		Destroy(this);
	}
}
