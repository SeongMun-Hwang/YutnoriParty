using Unity.Netcode;
using UnityEngine;

public class BasketGameController : NetworkBehaviour
{
    [SerializeField] private float moveSpeed = 5f;     

    private Vector3 targetPosition;
    private Animator animator;
    private bool canMove = true;

    private void Start()
    {
        Transform spawnTransform = FindFirstObjectByType<SpawnManager>().GetSpawnPosition(OwnerClientId);//요건 나중에 스폰위치관련으로 따로 수정할수도?
        targetPosition = spawnTransform.position;
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (!IsOwner || !canMove) return;

        Camera.main.transform.position = transform.position + new Vector3(0, 15, -5);
        Camera.main.transform.rotation = Quaternion.Euler(70f, 0f, 0f);


        Vector3 moveDirection = Vector3.zero;

        if (Input.GetKey(KeyCode.UpArrow)) moveDirection += Vector3.forward; 
        if (Input.GetKey(KeyCode.DownArrow)) moveDirection += Vector3.back;     
        if (Input.GetKey(KeyCode.LeftArrow)) moveDirection += Vector3.left;    
        if (Input.GetKey(KeyCode.RightArrow)) moveDirection += Vector3.right;   

        
        if (moveDirection != Vector3.zero)
        {
            moveDirection.Normalize(); 
            MoveServerRpc(OwnerClientId, moveDirection);
        }

        // 현재 위치와 목표 위치를 보간하여 이동
        transform.position = Vector3.Lerp(transform.position, targetPosition, moveSpeed * Time.deltaTime);

        // 즉시 이동 방향을 바라보도록 회전 처리
        if (moveDirection != Vector3.zero)
        {
            transform.forward = moveDirection;  // 즉시 회전
        }

      
        float moveSpeedValue = Vector3.Distance(transform.position, targetPosition) > 0.01f ? 1f : 0f;
        animator.SetFloat("MoveSpeed", moveSpeedValue);
    }

   
    [ServerRpc]
    private void MoveServerRpc(ulong clientId, Vector3 moveDirection)
    {
        if (OwnerClientId != clientId) return;

       
        targetPosition += moveDirection * moveSpeed * Time.deltaTime;
        MoveClientRpc(targetPosition, moveDirection);
    }

   
    [ClientRpc]
    private void MoveClientRpc(Vector3 newPosition, Vector3 moveDirection)
    {
        targetPosition = newPosition;

     
        if (moveDirection != Vector3.zero)
        {
            transform.forward = moveDirection; 
        }
    }

    public void EnableControl(bool enable)
    {
        canMove = enable;
    }
}