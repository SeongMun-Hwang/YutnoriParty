using Unity.Netcode;
using UnityEngine;

public class RunGameController : NetworkBehaviour
{
    [SerializeField] private float moveDistance = 2f;  // 한 번 이동할 거리
    [SerializeField] private float moveSpeed = 5f;     // 이동 속도

    private Vector3 targetPosition;
    private Animator animator;
    private bool canMove = false;
    public bool IsEliminated { get; private set; } = false;
    private void Start()
    {
        Transform spawnTransform = FindFirstObjectByType<SpawnManager>().GetSpawnPosition(OwnerClientId);//플레이어순서대로 배치
        targetPosition = spawnTransform.position;
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (!IsOwner || !canMove || IsEliminated) return;
        Camera.main.transform.position = transform.position + new Vector3(0, 4, 7);
        Camera.main.transform.rotation = Quaternion.Euler(6f, -180f, 0f);
        if (Input.GetKeyDown(KeyCode.Space))
        {
            MoveForwardServerRpc(OwnerClientId);  
        }

        transform.position = Vector3.Lerp(transform.position, targetPosition, moveSpeed * Time.deltaTime);
        float moveSpeedValue = Vector3.Distance(transform.position, targetPosition) > 0.01f ? 1f : 0f;
        animator.SetFloat("MoveSpeed", moveSpeedValue);
    }

    [ServerRpc]
    private void MoveForwardServerRpc(ulong clientId)
    {

        if (OwnerClientId != clientId || IsEliminated) return;

        targetPosition += transform.forward * moveDistance;
        MoveClientRpc(targetPosition);
    }

    [ClientRpc]
    private void MoveClientRpc(Vector3 newPosition)
    {
        targetPosition = newPosition;
    }
    public void EnableControl(bool enable)
    {
        canMove = enable;
    }
    public void SetEliminated(bool isEliminated)
    {
        IsEliminated = isEliminated;
        EnableControl(!isEliminated); // 탈락하면 false, 초기화되면 true
    }
}
