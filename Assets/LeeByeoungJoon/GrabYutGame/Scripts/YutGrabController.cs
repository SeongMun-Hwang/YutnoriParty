using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class YutGrabController : NetworkBehaviour
{
    public float result = 10000;
    bool isGrabbed = false;
    bool isSpacePressed = false;
    bool isPlaying = false;
    bool inRange = false;
    bool resultSent = false;
    float gravity = 4f;
    float minYutHeight = 4;
    float maxYutHeight = 8;
    float length = 10;
    [SerializeField] Animator animator;
    [SerializeField] LongYut yutPrefab;
    [SerializeField] Transform yutTransform;
    [SerializeField] Transform handPos;
    NetworkObject yutNo;
    LongYut yut;

    public override void OnNetworkSpawn()
    {
        if(!IsOwner) return;
        SpawnYutRpc(OwnerClientId);
    }

    [Rpc(SendTo.Server)]
    void SpawnYutRpc(ulong clientId)
    {
        Vector3 yutPos = new Vector3(yutTransform.position.x, Random.Range(minYutHeight, maxYutHeight), yutTransform.position.z);
        yutNo = Instantiate(yutPrefab).GetComponent<NetworkObject>();
        yutNo.transform.position = yutPos;
        yutNo.SpawnWithOwnership(OwnerClientId);
        //yutNo.TrySetParent(gameObject);
        SetYutRpc(yutNo);
    }

    [Rpc(SendTo.Owner)]
    void SetYutRpc(NetworkObjectReference noRef)
    {
        if(!noRef.TryGet(out NetworkObject no))
        {
            //Debug.Log("네트워크 오브젝트 못찾음");
            return;
        }

        yutNo = no;
        yut = yutNo.GetComponent<LongYut>();
        //Debug.Log("윷 할당함 : " + yutNo.NetworkObjectId);
    }

    public override void OnNetworkDespawn()
    {
        if(!IsServer) return;
        yutNo.Despawn();
    }

    void Update()
    {
        if (!isPlaying) return;
        //오너만 계산
        if (IsOwner)
        {
            length = yut.yutTop.position.y - handPos.position.y;
            //Debug.Log(NetworkManager.Singleton.LocalClientId + "번 플레이어 기록 : " + length);
            
            if(yut.yutBottom.position.y < handPos.position.y + 0.0236f && length > 0)
            {
                //Debug.Log("범위 안임");
                inRange = true;
            }
            else
            {
                //Debug.Log("범위 밖임");
                inRange = false;
            }

            //손 아래로 떨어져버리면 기회 없음
            if (length < 0 && !resultSent)
            {
                SendResult();
                //Debug.Log("놓침!");
            }

            //이미 스페이스바 눌렀으면 조작 불가
            if (!isSpacePressed)
            {
                //스페이스바 or 클릭 누르면 윷을 잡음
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
                {
                    //Debug.Log("스페이스바 누름");
                    GrabYut();
                }
            }

            //잡았으면 윷 떨어지는거 멈춤
            //못잡으면 윷 계속 떨어지게 하고싶은데 흠
            if (!isGrabbed)
            {
                //Debug.Log("윷 내려가라");
                yut.transform.Translate(gravity * Time.deltaTime * Vector3.left);
            }
        }
    }

    [Rpc(SendTo.Owner)]
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

        //손 범위 안에 들어왔을때만 기록
        if (inRange)
        {
            result = length;
            Debug.Log("기록 : " + result);

            isGrabbed = true;
            AudioManager.instance.Playsfx(18); //본인한테만 소리 재생
            Debug.Log("잡음!");
        }

        SendResult();
    }

    void SendResult()
    {
        //isPlaying = false;
        resultSent = true;

        //잡으면 기록
        if (isGrabbed)
        {
            YutGrabGameManager.Instance.SendReultRpc(result, NetworkManager.Singleton.LocalClientId);
        }

        YutGrabGameManager.Instance.NoChanceRpc();
    }
}
