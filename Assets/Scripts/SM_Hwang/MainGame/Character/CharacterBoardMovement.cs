using TMPro;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class CharacterBoardMovement : NetworkBehaviour
{
    private Animator animator;
    Node currentNode;
    Vector3 targetPos;
    float moveSpeed = 10f;
    bool isMoving = false;

    private void Start()
    {
        targetPos = new Vector3(10, 0, -6);
        animator = GetComponent<Animator>();
    }
    void Update()
    {
        if (!IsOwner) return;
        if (Input.GetMouseButtonDown(0) && !isMoving)
        {
            MoveToNextNode();
        }
        if (Input.GetMouseButtonDown(1) && !isMoving)
        {
            MoveToPrevNode();
        }
    }
    private void FixedUpdate()
    {
        if (!IsOwner) return;
        if (isMoving)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
            if (Vector3.Distance(transform.position, targetPos) < 0.01f)
            {
                transform.position = targetPos;
                isMoving = false;
                animator.SetFloat("isMoving", isMoving ? 1f : 0f);
            }
        }
    }
    public void MoveToNextNode()
    {
        if (currentNode == null)
        {
            targetPos.y = transform.position.y;
        }
        else
        {
            targetPos = currentNode.nextNode[Random.Range(0, currentNode.nextNode.Count)].transform.position;
            targetPos.y = transform.position.y;
        }
        isMoving = true;
        animator.SetFloat("isMoving", isMoving ? 1f : 0f);
    }
    public void MoveToPrevNode()
    {
        targetPos = currentNode.prevNode[Random.Range(0, currentNode.prevNode.Count)].transform.position;
        targetPos.y = transform.position.y;
        isMoving = true;
        animator.SetFloat("isMoving", isMoving ? 1f : 0f);
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Node"))
        {
            currentNode = other.GetComponent<Node>();
        }
    }
}