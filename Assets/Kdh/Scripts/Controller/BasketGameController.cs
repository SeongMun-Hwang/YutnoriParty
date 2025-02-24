using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
public class BasketGameController : NetworkBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private GameObject basketObject;
    private Vector3 targetPosition;
    private Animator animator; 
    private bool canMove = true;//테스트때매 true한것 매니저에서 true해줘야함 
    private string currentSceneName;

    private void Start()
    {
        currentSceneName = SceneManager.GetActiveScene().name;
        Transform spawnTransform = FindFirstObjectByType<SpawnManager>().GetSpawnPosition(OwnerClientId);
        targetPosition = spawnTransform.position;
        animator = GetComponent<Animator>();
        UpdateBasketObjectState();
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
        UpdateBasketObjectState();
    }
    private void UpdateBasketObjectState()
    {
        if (basketObject != null)
        {
            // "BasketGame" 씬일 때만 Basket 오브젝트를 활성화
            basketObject.SetActive(currentSceneName == "BasketGame");
        }
    }


    private void Update()
    {
        if (!IsOwner || !canMove || SceneManager.GetActiveScene().name != "BasketGame") return;

        //Camera.main.transform.position = transform.position + new Vector3(0, 15, -5);
        //Camera.main.transform.rotation = Quaternion.Euler(70f, 0f, 0f);// 카메라 지울지 말지 고민중


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