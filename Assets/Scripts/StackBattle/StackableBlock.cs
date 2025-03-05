using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class StackableBlock : NetworkBehaviour
{
	// 스포너 관련
	public BlockSpawnHandler spawner;
	public StackBattleManager manager;

	// StackableBlock 이동 관련
	private float distance; // 현재 블록이 원점으로부터 떨어진 거리
	private float maxDistance = 6f; // 블록이 최대로 멀어지는 거리
	public float moveSpeed = 6f; // 블록이 이동하는 속도
	private float stepLength; // 이동 거리
	private bool moveForward; // 정면으로 이동중인가?
	public bool moveX; // X방향으로 이동중인가?

	private NetworkVariable<Vector3> position = new NetworkVariable<Vector3>(
		Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
	);

	// 고정관련
	private bool isFixed;

	public override void OnNetworkSpawn()
	{
		distance = maxDistance;
		moveForward = false;

		if (IsServer) // 서버(Host)에서 초기 위치 설정
		{
			position.Value = transform.position;
		}

		if (moveX)
			transform.Translate(distance, 0, 0);
		else
			transform.Translate(0, 0, distance);
	}

	private void Update()
	{
		stepLength = Time.deltaTime * moveSpeed;

		if (!isFixed)
		{
			if (IsServer) // Host만 이동 로직 실행
			{
				if (moveX)
					MoveX();
				else
					MoveZ();

				position.Value = transform.position; // 이동 후 위치 업데이트
			}
			else if (IsClient)
			{
				transform.position = position.Value;
			}
		}
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
		if (!IsOwner) return;

		isFixed = true;
		if (Mathf.Abs(distance) > 0.2f)
		{
			float lostLength = Mathf.Abs(distance);

			if (moveX)
			{
				if (transform.localScale.x < lostLength)
				{
					Debug.Log("GameOver");
					manager.GameOver();
					isFixed = false;
					return;
				}

				transform.localScale -= new Vector3(lostLength, 0, 0);
				transform.Translate((distance > 0 ? -1 : 1) * lostLength / 2, 0, 0);
			}
			else
			{
				if (transform.localScale.z < lostLength)
				{
					Debug.Log("GameOver");
					manager.GameOver();
					isFixed = false;
					return;
				}

				transform.localScale -= new Vector3(0, 0, lostLength);
				transform.Translate(0, 0, (distance > 0 ? -1 : 1) * lostLength / 2);
			}
		}
		else
		{
			if (moveX)
				transform.Translate(-distance, 0, 0);
			else
				transform.Translate(0, 0, -distance);
		}

		FixBlockServerRpc(transform.position, transform.localScale);
	}

	[ServerRpc]
	public void FixBlockServerRpc(Vector3 pos, Vector3 scale)
	{
		if (!IsServer) return;
		transform.position = pos;
		transform.localScale = scale;
		FixBlockClientRpc(transform.position, scale);
		MoveCamera();
	}

	[ClientRpc]
	public void FixBlockClientRpc(Vector3 pos, Vector3 scale)
	{
		if (IsServer) return;
		// Debug.Log("클라이언트 고정");
		isFixed = true;
		transform.position = pos;
		transform.localScale = scale;
		MoveCamera();
	}

	public void MoveCamera()
	{
		StartCoroutine(MoveCameraCoroutine());
	}

	public IEnumerator MoveCameraCoroutine()
	{
		float moveLength = 1.0f;
		GameObject camera = GameObject.FindGameObjectWithTag("MainCamera");
		while (moveLength > 0)
		{
			float stepLength = 0.1f;
			moveLength -= stepLength;
			camera.transform.Translate(0, stepLength, 0, Space.World);
			yield return new WaitForSeconds(0.05f);
		}
	}
}
