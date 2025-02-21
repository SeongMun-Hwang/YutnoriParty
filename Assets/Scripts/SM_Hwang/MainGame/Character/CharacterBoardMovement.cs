using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CharacterBoardMovement : NetworkBehaviour
{
    private Animator animator;
    private Node selectedNode;
    Node currentNode;
    Vector3 targetPos;
    float moveSpeed = 10f;
    [SerializeField] GameObject characterOnDes;
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
    /*정방향 이동*/
    public void MoveToNextNode(int distance = 1)
    {
        StartCoroutine(MoveToTargetPos(distance));
    }
    /*역방향 이동*/
    public void MoveToPrevNode(int distance = 1)
    {
        StartCoroutine(MoveToTargetPos(distance));
    }
    private IEnumerator MoveToTargetPos(int distance)
    {
        animator.SetFloat("isMoving", 1f);
        int moveCount=Mathf.Abs(distance);
        for (int i = 0; i < moveCount; i++)
        {
            Node tmpNode = null;
            List<Node> possibleNodes = distance>0 ? currentNode.GetNextNode() : currentNode.GetPrevNode();

            if (possibleNodes.Count > 1)
            {
                yield return StartCoroutine(SpawnAndSelectNode(possibleNodes));
                tmpNode = selectedNode;
            }
            else
            {
                tmpNode = possibleNodes.Count == 1 ? possibleNodes[0] : null;
            }

            if (tmpNode == null)
            {
                EnterGoal();
                yield break;
            }

            targetPos = tmpNode.transform.position;
            targetPos.y = transform.position.y;

            while (Vector3.Distance(transform.position, targetPos) > 0.01f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
                yield return null;
            }
            transform.position = targetPos;
            currentNode = tmpNode;
        }
        Debug.Log("End move");
        GameManager.Instance.mainGameProgress.EndMove();
        animator.SetFloat("isMoving", 0f);
        yield break;
    }
    private IEnumerator SpawnAndSelectNode(List<Node> possibleNodes)
    {
        List<GameObject> spawnedDesObjects = new List<GameObject>();
        Dictionary<GameObject, Node> objectToNodeMap = new Dictionary<GameObject, Node>();

        foreach (Node node in possibleNodes)
        {
            GameObject desInstance = Instantiate(characterOnDes, node.transform.position, Quaternion.identity);
            spawnedDesObjects.Add(desInstance);
            objectToNodeMap[desInstance] = node;
        }

        selectedNode = null;
        while (selectedNode == null)
        {
            if (Input.GetMouseButtonDown(0)) // 마우스 클릭 감지
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    if (objectToNodeMap.TryGetValue(hit.collider.gameObject, out selectedNode))
                    {
                        break;
                    }
                }
            }
            yield return null;
        }

        foreach (GameObject desInstance in spawnedDesObjects)
        {
            Destroy(desInstance);
        }
    }

    private void EnterGoal()
    {
        Debug.Log("EnterGoal");
        gameObject.GetComponent<NetworkObject>().Despawn();
        Destroy(gameObject);
    }
}