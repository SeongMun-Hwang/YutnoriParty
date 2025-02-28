using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
public class BasketGameController : NetworkBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    
    private Vector3 targetPosition;
    Rigidbody rb;
    private Animator animator;
    private bool canMove = false;
    private string currentSceneName;
    [SerializeField] private int playerScore = 0;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        currentSceneName = SceneManager.GetActiveScene().name;
        Transform spawnTransform = FindFirstObjectByType<SpawnManager>().GetSpawnPosition(OwnerClientId);
        targetPosition = spawnTransform.position;
        animator = GetComponent<Animator>();
        
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded; 
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 씬이 바뀌면 새로운 씬 이름을 가져와서 업데이트
        currentSceneName = SceneManager.GetActiveScene().name;
        // 새로운 씬에 맞는 스폰 포인트로 위치 업데이트
        Transform spawnTransform = FindFirstObjectByType<SpawnManager>().GetSpawnPosition(OwnerClientId);
        targetPosition = spawnTransform.position;
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;  
            rb.position = targetPosition; 
        }
        else
        {
            transform.position = targetPosition;
        }
    }



    private void Update()
    {
        if (!IsOwner || !canMove || SceneManager.GetActiveScene().name != "BasketGame") return;

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
        canMove = enable;
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