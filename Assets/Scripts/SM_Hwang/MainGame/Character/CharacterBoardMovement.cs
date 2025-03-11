using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CharacterBoardMovement : MonoBehaviour
{
    private CharacterInfo characterInfo;
    private Animator animator;
    private Node selectedNode;
    private bool meetObstacle = false;
    Node currentNode;
    Vector3 targetPos;
    float moveSpeed = 10f;
    private void Awake()
    {
        StartCoroutine(WaitForStartNode());
        animator = GetComponent<Animator>();
        characterInfo = GetComponent<CharacterInfo>();
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
        StartCoroutine(MoveToTargetPos(distance));
    }
    /*말 이동 코루틴*/
    private IEnumerator MoveToTargetPos(int distance)
    {
        Debug.Log("Start To Move");
        GetComponent<Animator>().SetFloat("isMoving", 1f); //걷기 애니메이션 시작
        PlayerManager.Instance.isMoving = true;
        int moveCount = Mathf.Abs(distance);

        if (characterInfo.isReverse.Value)
        {
            distance *= -1;
            ItemManager.Instance.SetItemServerRpc(GetComponent<NetworkObject>(),false);
        }

        //한 칸 씩 전진
        for (int i = 0; i < moveCount; i++)
        {
            Node tmpNode = null; //다음 목표노드 저장 변수
            //현재 노드에서 앞 또는 뒤 노드리스트 tmpNode에 저장
            List<Node> possibleNodes = distance > 0 ? currentNode.GetNextNode() : currentNode.GetPrevNode();
            //갈림길이고 아직 한 걸을도 움직이지 않았을 때
            if (possibleNodes.Count > 1 && i == 0)
            {
                //갈림길 선택 대기
                yield return StartCoroutine(SpawnAndSelectNode(possibleNodes));
                tmpNode = selectedNode;
            }
            else
            {
                if (possibleNodes.Count > 0) //이동 가능한 하나이고(외길)
                {
                    if (distance > 0) //전진이면
                    {
                        tmpNode = possibleNodes[0]; //possibleNode가 하나니 0 저장
                    }
                    else
                    { //후진이면
                        if (currentNode.GetPrevNode()[0] == currentNode) //이전 노드와 현재 노드가 같으면 == 시작지점
                        {
                            Debug.Log("Prev Node is null");
                            tmpNode = currentNode.GetNextNode()[0]; //방향을 앞으로 설정
                        }
                        else //이전 노드가 있으면
                        {
                            Debug.Log("Prev Node not null");
                            tmpNode = currentNode.GetPrevNode()[0];
                        }
                    }
                }
                else //이동 가능한 길이 없으면
                {
                    tmpNode = null;
                }
            }
            //다음 노드가 null == 골인 지점. 골인 후 코루틴 탈출
            if (tmpNode == null)
            {
                PlayerManager.Instance.CharacterGoalIn(gameObject);
                GameManager.Instance.mainGameProgress.EndMove();
                yield break;
            }

            //tmp노드 다음 목적지로 설정
            targetPos = tmpNode.transform.position;
            targetPos.y = transform.position.y;

            Vector3 direction = (targetPos - transform.position).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(direction);

            //tmpNode 방향으로 회전
            while (Quaternion.Angle(transform.rotation, targetRotation) > 0.1f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.deltaTime);
                yield return null;
            }
            transform.rotation = targetRotation;

            //이동 시작
            while (Vector3.Distance(transform.position, targetPos) > 0.01f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
                yield return null;
            }
            transform.position = targetPos;

            //이동이 끝났으니 현재 노드를 목표로 했던 노드로 변경
            currentNode = tmpNode;
            if (meetObstacle)
            {
                Debug.Log("meet obstacle");
                meetObstacle = false;
                break;
            }
        }
        //이동 종료 후 처리
        PlayerManager.Instance.isMoving = false;
        animator.SetFloat("isMoving", 0f);
        //yield return new WaitForSeconds(0.5f);
        GameManager.Instance.mainGameProgress.EndMove();
    }
    /*이동 가능 목적지에 캐릭터 복제, 마우스로 클릭*/
    private IEnumerator SpawnAndSelectNode(List<Node> possibleNodes)
    {
        List<GameObject> spawnedDesObjects = new List<GameObject>();
        Dictionary<GameObject, Node> objectToNodeMap = new Dictionary<GameObject, Node>();

        //가능한 모든 경로에 자신 복제
        foreach (Node node in possibleNodes)
        {
            GameObject desInstance = Instantiate(gameObject, node.transform.position, Quaternion.identity);
            spawnedDesObjects.Add(desInstance);
            objectToNodeMap[desInstance] = node;
        }
        //selectedNode 비움
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
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Obstacle"))
        {
            meetObstacle = true;
            MainGameProgress.Instance.DespawnNetworkObjectServerRpc(other.gameObject);
            Debug.Log("Obstacle!");
        }
    }
}