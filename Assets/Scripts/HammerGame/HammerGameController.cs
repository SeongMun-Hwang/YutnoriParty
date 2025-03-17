using UnityEngine;
using Unity.Netcode;
using UnityEngine.EventSystems;

public class HammerGameController : NetworkBehaviour
{
    [SerializeField] Camera mainCamera;
    [SerializeField] GameObject cameraParent;
    [SerializeField] SkinnedMeshRenderer characterRenderer;
    public float moveSpeed = 5f;
    public float mouseSensitivity = 2f;
    private bool isAttacked = false;
    private Animator animator;
    private float rotationX = 0f;
    private float rotationY = 0f;
    private Vector3 moveDirection;
    private bool isHammerGameStart = false;
    void Start()
    {
        animator = GetComponent<Animator>();
        Cursor.lockState = CursorLockMode.Locked;
    }
    public override void OnNetworkSpawn()
    {
        if (OwnerClientId == NetworkManager.LocalClientId)
        {
            Debug.Log("set camera");
            mainCamera.gameObject.SetActive(true);
            mainCamera = Camera.main;
            characterRenderer.enabled = false;
        }
    }

    void Update()
    {
        if (!IsOwner) return;
        RotateWithMouse();
        if (isHammerGameStart) { 
        MoveCharacter();
        HammerAttack();
    }
    }
    private void FixedUpdate()
    {
        if (!IsOwner) return;
        if (moveDirection != Vector3.zero)
        {
            GetComponent<Rigidbody>().linearVelocity = moveDirection.normalized * moveSpeed + new Vector3(0, GetComponent<Rigidbody>().linearVelocity.y, 0);
        }
        else
        {
            GetComponent<Rigidbody>().linearVelocity = new Vector3(0, GetComponent<Rigidbody>().linearVelocity.y, 0);
        }
    }
    private void RotateWithMouse()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mousey = Input.GetAxis("Mouse Y") * mouseSensitivity;

        rotationY += mouseX;
        transform.rotation = Quaternion.Euler(0f, rotationY, 0f);

        rotationX -= mousey;
        rotationX = Mathf.Clamp(rotationX, -60f, 60f);
        cameraParent.transform.localRotation = Quaternion.Euler(rotationX, 0f, 0f);
    }

    private void MoveCharacter()
    {
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        moveDirection = transform.right * moveX + transform.forward * moveZ;
        animator.SetFloat("moveSpeed", moveDirection.magnitude);
    }
    private void HammerAttack()
    {
        if (Input.GetMouseButtonDown(0) && !isAttacked)
        {
            animator.SetTrigger("Attack");
            animator.SetFloat("moveSpeed", 0);
            isAttacked = true;
            GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
        }
    }
    public void IsAttackFinished()
    {
        isAttacked = false;
        animator.SetTrigger("Idle");
    }
    [ClientRpc]
    public void StartHammerGameClientRpc()
    {
        isHammerGameStart = true;
    }
}
