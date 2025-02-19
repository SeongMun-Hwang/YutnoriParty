using System.Collections;
using Unity.Netcode;
using UnityEngine;

enum CharacterMove
{
    Forward,
    Backward
}
public class CharacterBoardMovement : NetworkBehaviour
{
    private Animator animator;
    Node currentNode;
    Vector3 targetPos;
    float moveSpeed = 10f;

    private void Start()
    {
        StartCoroutine(WaitForStartNode());
        animator = GetComponent<Animator>();
    }
    private IEnumerator WaitForStartNode()
    {
        while (GameManager.Instance == null)
        {
            yield return null;
        }
        currentNode = GameManager.Instance.startNode;
    }
    public void MoveToNextNode(int distance = 1)
    {
        StartCoroutine(MoveToTargetPos(distance, CharacterMove.Forward));
    }
    public void MoveToPrevNode(int distance = 1)
    {
        StartCoroutine(MoveToTargetPos(distance, CharacterMove.Backward));
    }
    private IEnumerator MoveToTargetPos(int distance, CharacterMove dir)
    {
        animator.SetFloat("isMoving", 1f);

        for (int i = 0; i < distance; i++)
        {
            Node tmpNode = null;
            if (dir == CharacterMove.Forward) tmpNode = currentNode.GetNextNode();
            else if (dir == CharacterMove.Backward) tmpNode = currentNode.GetPrevNode();
            if(tmpNode == null)
            {
                EnterGoal();
                yield break;
            }
            targetPos=tmpNode.transform.position;

            targetPos.y = transform.position.y;
            while (Vector3.Distance(transform.position, targetPos) > 0.01f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
                yield return null;
            }
            transform.position = targetPos;
            currentNode = tmpNode;
        }
        animator.SetFloat("isMoving", 0f);
    }
    private void EnterGoal()
    {
        Debug.Log("EnterGoal");
        Destroy(gameObject);
    }
}