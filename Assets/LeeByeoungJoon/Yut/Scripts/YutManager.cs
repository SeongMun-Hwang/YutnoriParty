using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public enum YutResult
{
    Do,
    Gae,
    Gur,
    Yut,
    Mo,
    BackDo,
    Error
}

enum YutFace
{
    Back,
    Front,
    Error
}
public class YutManager : NetworkBehaviour
{
    [SerializeField] Yut yutPrefab;
    [SerializeField] Yut backDoYutPrefab;
    [SerializeField] GameObject yutResultContent;
    [SerializeField] YutResults yutResultPrefab;
    [SerializeField] Transform yutSpawnTransform;
    [SerializeField] LayerMask ground;
    [SerializeField] Image powerGauge;

    List<Yut> yuts = new List<Yut>();
    List<YutResult> results = new List<YutResult>();
    public List<YutResult> Results { get { return results; } }

    int faceDown = 0;
    int powerAmountSign = 1;
    float yutRaycastLength = 5;
    float minThrowPower = 8;
    float maxThrowPower = 20;
    float powerTimeOut = 3;
    float powerStartTime = 0;
    float torque = 3;
    float yutSpacing = 2;
    float waitTime = 10;
    float waitInterval = 1;
    float powerAmount = 0;
    bool backDo = false;
    bool isThrowButtonDown = false;

    public int yutNum = 4;
    public int throwChance = 0;

    //싱글톤 아님
    static YutManager instance;
    static public YutManager Instance
    {
        get
        {
            return instance;
        }
    }

    private void Awake()
    {
        instance = this;
    }

    public override void OnNetworkSpawn()
    {
        //서버에서만 윷 소환
        if (!IsServer) return;

        Vector3 pos = yutSpawnTransform.position;

        for (int i = 0; i < yutNum; i++)
        {
            //윷 소환하고
            if (i == 0)
            {
                //0번 윷은 백도 윷
                yuts.Add(Instantiate(backDoYutPrefab));
            }
            else
            {
                //나머지는 그냥 윷
                yuts.Add(Instantiate(yutPrefab));
            }

            Yut yut = yuts[i];
            //윷 전체의 중심 위치 맞추기 위한 똥꼬쇼
            yut.transform.position = pos + new Vector3(0, 0, -((yutNum - 1) * yutSpacing) / 2);
            yut.GetComponent<NetworkObject>().Spawn();

            //위치 잡아주고
            if (i > 0)
            {
                yut.transform.position = yuts[i - 1].transform.position + new Vector3(0, 0, yutSpacing);
            }

            //초기화된 위치 저장
            yut.originPos = yut.transform.position;
            yut.originRot = yut.transform.rotation;

            //안보이게 하기
            //yut.gameObject.SetActive(false);
        }
    }

    private void FixedUpdate()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            //MyTurn();
            YutResultCount();
        }

        if (isThrowButtonDown)
        {
            if (powerAmount >= 1)
            {
                powerAmountSign = -1;
            }
            else if (powerAmount <= 0)
            {
                powerAmountSign = 1;
            }
            powerAmount += Time.deltaTime * powerAmountSign;

            powerGauge.fillAmount = Mathf.Clamp(powerAmount, 0, 1);
            //Debug.Log("현재 파워 : " + powerAmount);

            //타임아웃되면 알아서 던짐
            if (Time.time - powerStartTime > powerTimeOut)
            {
                ThrowButtonReleased();
            }
        }
    }

    void MyTurn()
    {
        throwChance = 1;
        Debug.Log("내턴, 던질 기회 +1");
    }

    public void ThrowButtonPressed()
    {
        //지금 누구 턴인지
        //if ((ulong)GameManager.Instance.mainGameProgress.currentPlayerNumber != NetworkManager.LocalClientId)
        //{
        //    return;
        //}
        //던질 기회가 남았는지
        if (throwChance < 1)
        {
            Debug.Log("던질 기회 없음");
            return;
        }
        //누르고 있는 동안 파워 게이지 작동
        powerAmount = 0;
        powerStartTime = Time.time;

        isThrowButtonDown = true;
    }
    public void ThrowButtonReleased()
    {
        //버튼 풀려있으면 작동 안되게함
        if (!isThrowButtonDown) return;

        //버튼 풀면 파워게이지 멈추고
        isThrowButtonDown = false;

        //윷 몇개 던질지 확인하고, 현재 파워로 던짐
        ThrowYutsServerRpc(yutNum, Mathf.Clamp(maxThrowPower * powerAmount, minThrowPower, maxThrowPower), new ServerRpcParams());
        throwChance--;
        Debug.Log("던짐");
    }

    [ServerRpc(RequireOwnership = false)]
    void ThrowYutsServerRpc(int yutNums, float power, ServerRpcParams rpcParams)
    {
        backDo = false;
        faceDown = 0;
        for (int i = 0; i < yutNums; i++)
        {
            Yut yut = yuts[i];

            //윷을 원래 위치로 돌리기
            yut.transform.localPosition = yut.originPos; //외않됢?
            yut.transform.localRotation = yut.originRot; //외않됢? 이제 됢!

            //보이게 하기
            //yut.gameObject.SetActive(true);

            //윷 던지기
            //던지기 전에 움직임을 없애고(이상한 방향으로 날라가는거 방지)
            yut.Rigidbody.linearVelocity = Vector3.zero;
            yut.Rigidbody.angularVelocity = Vector3.zero;
            //윷에 힘을 가해 위쪽 방향으로 던지고, 랜덤한 토크를 가해 앞 뒷면을 조절한다
            yut.Rigidbody.AddForce(Vector3.up * power, ForceMode.Impulse);
            yut.Rigidbody.AddTorque(yut.transform.forward * Random.Range(-torque, torque), ForceMode.Impulse);
        }

        StartCoroutine(YutResultCheck(0, yutNums, rpcParams));
    }

    IEnumerator YutResultCheck(float timePassed, int yutNums, ServerRpcParams rpcParams)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;

        bool yutStable = false;
        YutFace[] faces = new YutFace[yutNums];

        //일정 시간동안 반복
        //waitTime 안에 결과가 안나오면 에러남
        while (timePassed < waitTime)
        {
            //1초마다 윷 상태를 확인
            yield return new WaitForSecondsRealtime(waitInterval);

            for (int i = 0; i < yutNums; i++)
            {
                Yut yut = yuts[i];

                //윷이 멈춰있으면 결과 확인 가능한걸로 판단 -> 완전히 안멈추면 결과 안나옴
                //윷이 수직으로 서있을때 레이캐스트 해버리는 상황 -> 앞 뒷면 레이캐스트를 쏴서 바닥에 면이 붙어있는지 체크
                if (yut.Rigidbody.linearVelocity == Vector3.zero && yut.Rigidbody.angularVelocity == Vector3.zero)
                {
                    //다 멈추면 true로 유지
                    yutStable = true;
                    faces[i] = CalcYutResult(yut);
                    Debug.Log(i + "번 윷 앞뒷면 : " + faces[i]);
                    //에러 뜨면 안정적이지 않다고 판정, 루프 지속
                    if (faces[i] == YutFace.Error)
                    {
                        yutStable = false;
                    }
                }
                else
                {
                    //하나라도 안멈춰있으면 루프 지속
                    yutStable = false;
                }
            }

            if (yutStable)
            {
                for (int i = 0; i < yutNums; i++)
                {
                    //레이캐스트 해서 앞뒷면 계산
                    faces[i] = CalcYutResult(yuts[i]);
                    //윷 결과 계산
                    if (faces[i] == YutFace.Back)
                    {
                        //백도 계산
                        if (i == 0)
                        {
                            backDo = true;
                        }

                        faceDown++;
                        //Debug.Log("뒷면 +1, 총 개수 : " + faceDown);
                    }
                }
                break;
            }

            timePassed += waitInterval;
        }

        //Debug.Log("총 개수 : " + faceDown);
        if (!yutStable)
        {
            Debug.Log("결과 산출 실패 : 타임아웃");
            //타임아웃나면 다시 던질 수 있게 기회 더 줌
            ThrowChanceChangeClientRpc(1, senderId);

            yield break;
        }

        switch (faceDown)
        {
            case 0:
                AddYutResultClientRpc(YutResult.Mo, senderId);
                ThrowChanceChangeClientRpc(1, senderId);
                break;
            case 1:
                if (backDo)
                {
                    AddYutResultClientRpc(YutResult.BackDo, senderId);
                    break;
                }
                AddYutResultClientRpc(YutResult.Do, senderId);
                break;
            case 2:
                AddYutResultClientRpc(YutResult.Gae, senderId);
                break;
            case 3:
                AddYutResultClientRpc(YutResult.Gur, senderId);
                break;
            case 4:
                AddYutResultClientRpc(YutResult.Yut, senderId);
                ThrowChanceChangeClientRpc(1, senderId);
                break;
            default:
                AddYutResultClientRpc(YutResult.Error, senderId);
                break;
        }
    }

    YutFace CalcYutResult(Yut yut)
    {
        bool isFront = YutRayCast(yut, true);
        bool isBack = YutRayCast(yut, false);
        Debug.Log("앞면 : " +  isFront + " 뒷면 : " +  isBack);

        //두 결과가 모두 달라야만 올바른 출력
        if (isFront ^ isBack) 
        {
            if (isFront)
            {
                return YutFace.Front;
            }   
            else
            {
                return YutFace.Back;
            }
        }
        //둘 다 같은거 나오면 정상적인 상태가 아니므로 오류출력
        return YutFace.Error;
    }

    bool YutRayCast(Yut yut, bool isFront)
    {
        Vector3 dir = yut.transform.right;
        Color color = Color.red;

        if (isFront)
        {
            dir *= -1;
            color = Color.green;
        }
        
        RaycastHit hit;
        Debug.DrawRay(yut.transform.position, dir * yutRaycastLength, color, 0.3f);
        if (Physics.Raycast(yut.transform.position, dir, out hit, yutRaycastLength, ground))
        {
            return true;
        }
        return false;
    }

    public int YutResultCount()
    {
        //현재 플레이어 id 체크
        //player2가 선턴을 잡을때 player1은 0이, player2는 1이 찍힘 == 플레이어 넘버가 동기화 안됨
        //Debug.Log("게임매니저 현재 플레이어 번호 : " + GameManager.Instance.mainGameProgress.currentPlayerNumber);
        //Debug.Log("클라이언트id " +NetworkManager.Singleton.LocalClientId + " 윷 리스트 카운트 : " + results.Count);
        return results.Count;
    }

    public List<YutResult> GetYutResults()
    {
        //현재 플레이어의 클라이언트 id 체크하고 리스트 반환?
        return results;
    }

    [ClientRpc]
    void AddYutResultClientRpc(YutResult result, ulong senderId)
    {
        //Debug.Log("로컬 클라이언트 id : " + NetworkManager.Singleton.LocalClientId + "\nrpc요청 id : " + senderId + "\n오너 클라이언트 id : " + OwnerClientId);

        //윷 던지는거 요청한 클라이언트의 윷 결과창을 갱신
        if (senderId == NetworkManager.Singleton.LocalClientId)
        {
            results.Add(result);
            Instantiate(yutResultPrefab, yutResultContent.transform).SetYutText(result);
        }
    }

    //리스트에서 윷 결과 삭제
    public void RemoveYutResult(YutResult result)
    {
        //Debug.Log("id : " + NetworkManager.Singleton.LocalClientId + "" + result + "삭제");
        results.Remove(result);
    }

    [ClientRpc]
    public void ClearYutResuliClientRpc()
    {
        //모든 클라이언트에서 리스트 클리어
        results.Clear();
    }

    [ClientRpc]
    public void ThrowChanceChangeClientRpc(int num, ulong senderId)
    {
        //해당 클라이언트의 윷 던지기 횟수를 갱신
        if(senderId == NetworkManager.Singleton.LocalClientId)
        {
            throwChance += num;
        }
    }
}
