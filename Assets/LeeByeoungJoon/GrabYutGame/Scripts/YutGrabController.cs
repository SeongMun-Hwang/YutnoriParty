using Unity.Netcode;
using UnityEngine;

public class YutGrabController : NetworkBehaviour
{
    public float result;
    bool isGrabbed = false;
    bool isSpacePressed = false;
    bool isPlaying = false;
    float gravity = 9.8f;
    float minYutHeight = 4;
    float maxYutHeight = 8;
    float length;
    [SerializeField] Animator animator;
    [SerializeField] GameObject yutPrefab;
    [SerializeField] Transform yutTop;
    [SerializeField] Transform handPos;
    public override void OnNetworkSpawn()
    {
        //윷 높이 초기화
        yutPrefab.transform.position = new Vector3(transform.position.x , Random.Range(minYutHeight, maxYutHeight), transform.localPosition.z);
    }

    void Update()
    {
        if (!isPlaying) return;

        length = yutTop.position.y - handPos.position.y; //손 아래로 떨어져버리면 기회 없음
        Debug.Log(NetworkManager.Singleton.LocalClientId + "번 플레이어 기록 : " + length);

        if(length < 0)
        {
            SendResult();
        }

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
            yutPrefab.transform.Translate(gravity * Time.deltaTime * Vector3.left);
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void GameStartRpc()
    {
        Debug.Log("윷 잡기 게임 시작함");
        isPlaying = true;
    }

    void GrabYut()
    {
        //오너만 조작 가능
        if (!IsOwner) return;

        Debug.Log("잡아라!");
        isSpacePressed = true;
        animator.SetBool("DoGrab", true);
        result = yutTop.transform.position.y - handPos.transform.position.y;
        
        //결과가 양수면 잡음
        if(result > 0)
        {
            isGrabbed = true;
            Debug.Log("잡음!");
        }

        SendResult();
    }

    void SendResult()
    {
        isPlaying = false;
        YutGrabGameManager.Instance.SendReultRpc(result);
        YutGrabGameManager.Instance.NoChanceRpc();
    }
}
