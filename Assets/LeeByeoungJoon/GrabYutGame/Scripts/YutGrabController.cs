using Unity.Netcode;
using UnityEngine;

public class YutGrabController : NetworkBehaviour
{
    public float result;
    bool isGrabbed = false;
    bool isSpacePressed = false;
    float gravity = 12f;
    float minYutHeight = 4;
    float maxYutHeight = 8;
    [SerializeField] Animator animator;
    [SerializeField] GameObject yutPrefab;
    [SerializeField] Transform yutTop;
    [SerializeField] Transform handPos;
    public override void OnNetworkSpawn()
    {
        //윷 높이 초기화
        yutPrefab.transform.position = new Vector3(transform.position.x, Random.Range(minYutHeight, maxYutHeight), transform.position.z);
    }

    void Update()
    {
        if (!YutGrabGameManager.Instance.isPlaying.Value) return;

        //이미 스페이스바 눌렀으면 조작 불가
        if (!isSpacePressed)
        {
            //스페이스바 누르면 윷을 잡음
            if (Input.GetKeyDown(KeyCode.Space))
            {
                GrabYut();
            }
        }

        //잡았으면 윷 떨어지는거 멈춤
        if (!isGrabbed)
        {
            Debug.Log("떨어진다");
            yutPrefab.transform.Translate(gravity * Time.deltaTime * Vector3.left);
        }
    }

    void GrabYut()
    {
        //오너만 조작 가능
        if (!IsOwner) return;

        isSpacePressed = true;
        animator.SetBool("DoGrab", true);
        result = yutTop.transform.position.y - handPos.transform.position.y;
        
        //결과가 양수면 잡음
        if(result > 0)
        {
            isGrabbed = true;
        }
    }
}
