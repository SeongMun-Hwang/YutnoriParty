using Unity.Netcode;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] private float moveDistance = 2f;  // 한 번 이동할 거리
    [SerializeField] private float moveSpeed = 5f;     // 이동 속도

    private Vector3 targetPosition;
    private Animator animator;
    private bool canMove = false;
    private void Start()
    {
        Transform spawnTransform = FindFirstObjectByType<SpawnManager>().GetSpawnPosition(OwnerClientId);
        targetPosition = spawnTransform.position;
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (!IsOwner || !canMove) return;
        Camera.main.transform.position = transform.position + new Vector3(0, 5, 5);
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
       
        if (OwnerClientId != clientId) return;

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
}
