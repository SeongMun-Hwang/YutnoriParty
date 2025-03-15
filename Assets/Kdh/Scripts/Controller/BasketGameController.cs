using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
public class BasketGameController : NetworkBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    
    private Vector3 targetPosition;
    Rigidbody rb;
    private Animator animator;
    public NetworkVariable<bool> canMove = new NetworkVariable<bool>(false);
    [SerializeField] private Vector3 spawnPosition;

    [SerializeField] private int playerScore = 0;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();        
        rb.position = targetPosition;
        animator = GetComponent<Animator>();
        
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            targetPosition = spawnPosition;
            transform.position = targetPosition;
        }
    }



    private void Update()
    {
        if (!IsOwner || !canMove.Value) return;

        float hAxis = Input.GetAxis("Horizontal");  
        float vAxis = Input.GetAxis("Vertical");   

        Vector3 moveDirection = new Vector3(hAxis, 0, vAxis).normalized; 

        if (moveDirection != Vector3.zero)
        {
            MoveServerRpc(OwnerClientId, moveDirection);           
        }

        if (rb != null)
        {
            rb.linearVelocity = new Vector3(moveDirection.x * moveSpeed, rb.linearVelocity.y, moveDirection.z * moveSpeed);
        }

        float moveSpeedValue = rb.linearVelocity.magnitude > 0.01f ? 1f : 0f;
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
        canMove.Value = enable;
    }

    [ServerRpc(RequireOwnership = false)]
    public void AddScoreServerRpc(ulong playerId, int points)
    {
        if (!IsServer) return;  

        // 점수 갱신 후 UI 업데이트
        BasketGameManager.Instance.AddScore(playerId, points);
    }

    public void AddScore(int scoreValue)
    {
        if (!IsOwner) return; 

        playerScore += scoreValue;
        

        // 점수를 서버로 요청
        AddScoreServerRpc(OwnerClientId, scoreValue);
    }


}