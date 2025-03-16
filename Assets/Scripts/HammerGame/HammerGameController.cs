using UnityEngine;
using Unity.Netcode;
using UnityEngine.EventSystems;

public class HammerGameController : NetworkBehaviour
{
    public float moveSpeed = 5f;
    public float mouseSensitivity = 2f;
    private bool isAttacked = false;
    private Animator animator;
    private Rigidbody rb;
    private float rotationY = 0f;

    void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        Cursor.lockState = CursorLockMode.Locked; // 마우스 커서 고정
    }

    void Update()
    {
        RotateWithMouse();
        MoveCharacter();

        if (Input.GetMouseButtonDown(0) && !isAttacked)
        {
            animator.SetTrigger("Attack");
            animator.SetFloat("moveSpeed", 0);
            isAttacked = true;
            rb.linearVelocity = Vector3.zero;
        }
    }

    private void RotateWithMouse()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        rotationY += mouseX;
        transform.rotation = Quaternion.Euler(0f, rotationY, 0f);
    }

    private void MoveCharacter()
    {
        if (!isAttacked)
        {
            float moveX = Input.GetAxis("Horizontal");
            float moveZ = Input.GetAxis("Vertical");

            Vector3 moveDirection = transform.right * moveX + transform.forward * moveZ;
            Debug.Log(moveDirection.magnitude);
            animator.SetFloat("moveSpeed", moveDirection.magnitude);
            rb.linearVelocity = moveDirection.normalized * moveSpeed + new Vector3(0, rb.linearVelocity.y, 0);
        }
    }

    public void IsAttackFinished()
    {
        isAttacked = false;
        animator.SetTrigger("Idle");
    }
}
