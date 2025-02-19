using Unity.Netcode;
using UnityEngine;

public class StackableBlock : NetworkBehaviour
{
	// 스포너 관련
	[SerializeField] GameObject lostPrefabs;
	public BlockSpawnHandler spawner;

	// StackableBlock 이동 관련

	// 현재 블록이 원점으로부터 떨어진 거리
	private NetworkVariable<float> distance = new NetworkVariable<float>(
		0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
	private float maxDistance = 6f; // 블록이 최대로 멀어지는 거리
	private float stepLength; // 이동 거리

	private bool moveForward; // 정면으로 이동중인가?
	public bool moveX; // X방향으로 이동중인가?
	// 고정되었는가?
	private NetworkVariable<bool> isFixed = new NetworkVariable<bool>(
		false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
	private NetworkVariable<Vector3> position = new NetworkVariable<Vector3>(
		Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

	public override void OnNetworkSpawn()
	{
		moveForward = false;

		if (IsServer)
		{
			float y = transform.position.y;
			if (moveX)
			{ position.Value = new Vector3(maxDistance, y, 0); }
			else
			{ position.Value = new Vector3(0, y, maxDistance); }

			distance.Value = maxDistance;
		}

		transform.position = position.Value;
	}

	private void Update()
	{
		if (!IsServer) return; // 서버에서만 이동 계산

		stepLength = Time.deltaTime * 6f;

		if (!isFixed.Value)
		{
			if (moveX)
			{ MoveX(); }
			else
			{ MoveZ(); }
		}
	
		position.Value = transform.position;
	}

	// X방향으로 이동
	private void MoveX()
	{
		if (moveForward)
		{
			if (position.Value.x < maxDistance)
			{
				transform.Translate(stepLength, 0, 0);
				distance.Value += stepLength;
			}
			else
			{ moveForward = false; }
		}
		else
		{
			if (position.Value.x > -maxDistance)
			{
				transform.Translate(-stepLength, 0, 0);
				distance.Value -= stepLength;
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
			if (position.Value.z < maxDistance)
			{
				transform.Translate(0, 0, stepLength);
				distance.Value += stepLength;
			}
			else
			{ moveForward = false; }
		}
		else
		{
			if (position.Value.z > -maxDistance)
			{
				transform.Translate(0, 0, -stepLength);
				distance.Value -= stepLength;
			}
			else
			{ moveForward = true; }
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void ScaleBlockServerRpc()
	{
		if (!IsServer)
			return; // 서버에서만 실행

		isFixed.Value = true;
		ScaleBlockClientRpc(transform.localScale, transform.position);
	}

	// 클라이언트에 변경사항 전파
	[ClientRpc]
	private void ScaleBlockClientRpc(Vector3 newScale, Vector3 newPosition)
	{
		if (IsServer)
			return; // 서버는 이미 처리했으므로 클라이언트만 실행

		transform.localScale = newScale;
		transform.position = newPosition;
		
		Debug.Log("블록 배치 : 클라이언트");
	}

	private void LateUpdate()
	{
		if (!IsServer)
		{
			transform.position = position.Value; // 클라이언트는 서버 값 따라가기
		}
	}
}
