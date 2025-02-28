using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
public class RunGameController : NetworkBehaviour
{
    [SerializeField] private float moveDistance = 2f;  // 한 번 이동할 거리
    [SerializeField] private float moveSpeed = 5f;     // 이동 속도

    private Vector3 targetPosition;
    private Animator animator;
    private bool canMove = false;
    private string currentSceneName;
    Rigidbody rb;
    public bool IsEliminated { get; private set; } = false;
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
        transform.position = targetPosition;

    }

  

    private void Update()
    {
        string sceneName = SceneManager.GetActiveScene().name;

        if (sceneName == "RunGame")
        {
            rb.isKinematic = true;
        }
        else if (sceneName == "BasketGame")  
        {
            rb.isKinematic = false;
        }
        if (!IsOwner || !canMove || IsEliminated || SceneManager.GetActiveScene().name != "RunGame") return;
        
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
