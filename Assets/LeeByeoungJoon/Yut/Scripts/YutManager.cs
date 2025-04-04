using System.Collections;
using System.Collections.Generic;
using TMPro;
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
    [SerializeField] RectTransform viewport;
    [SerializeField] GameObject yutResultContent;
    [SerializeField] YutResults yutResultPrefab;
    [SerializeField] YutOut outCollision;
    //[SerializeField] YutOutTrigger outTrigger;
    [SerializeField] Transform yutSpawnTransform;
    [SerializeField] LayerMask ground;
    [SerializeField] Image powerGauge;
    [SerializeField] TextMeshProUGUI throwChanceTmp;
    List<Yut> yuts = new List<Yut>();
    //List<YutResult> results = new List<YutResult>();
    //public List<YutResult> Results { get { return results; } }

    List<YutResults> results = new List<YutResults>();
    public List<YutResults> Results { get { return results; } }

    int faceDown = 0;
    int powerAmountSign = 1;
    float yutRaycastLength = 10;
    public float minThrowPower = 220; //최소 파워
    public float maxThrowPower = 320; //최대 파워(파워 계산은 얘 기반이라 이걸로 조절)
    float powerTimeOut = 3; //자동으로 던져지는 시간
    float powerStartTime = 0;
    public float minTorque = 6; //최소 토크
    public float maxTorque = 10; //최대 토크
    float yutSpacing = 2;
    float waitTime = 10;
    float waitInterval = 0.5f;
    float powerAmount = 0;
    bool autoYut = false;
    bool isThrower = false;
    bool backDo = false;
    bool isThrowButtonDown = false;
    bool isFaceError = false;
    bool _isYutFalled = false;
    public bool isYutFalled
    {
        get { return _isYutFalled; }
        set
        {
            if( _isYutFalled != value)
            {
                Debug.Log($"isYutFalled 변경 : {_isYutFalled} -> {value}");
                _isYutFalled = value;
            }
        }
    }
    bool faceStable = false;

    public int yutNum = 4;
    public bool isCalulating = false;

    private int _throwChance = 0;
    public int throwChance{
        get { return _throwChance; }
        set
        {
            if(_throwChance!=value)
            {
                _throwChance = value;
                OnThrowChangeChanged();
            }
        }
    }

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
        //서버에서만 낙 트리거 판정
        if (!IsServer) return;

        //윷 소환
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

            yut.soundActivated = true;

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

            //작은 움직임이 남아도 멈춘걸로 인식하도록 값 조절
            //yut.Rigidbody.sleepThreshold = yutSleepThreshold;
        }

        //낙 트리거 등록
        outCollision.OnYutCollided += YutFalled;
    }

    [Rpc(SendTo.Server)]
    public void HideYutRpc()
    {
        List<NetworkObject> yutList = new List<NetworkObject>();

        for (int i=0; i< yutNum; i++)
        {
            Yut yut = yuts[i];
            yut.soundActivated = false;
            //yutList.Add(yut.GetComponent<NetworkObject>());
            //yut.GetComponent<NetworkObject>().NetworkHide((ulong)i);
            //yut.gameObject.SetActive(false);
            yut.transform.localPosition = yut.originPos + new Vector3(0,-10,0);
        }

        //foreach(var client in NetworkManager.Singleton.ConnectedClients)
        //{
        //    //서버는 패스
        //    if (client.Key == NetworkManager.ServerClientId) continue;

        //    foreach(var yut in yutList)
        //    {
        //        //이미 hide 되어있으면 패스
        //        if (!yut.IsNetworkVisibleTo(client.Key)) continue;

        //        //감추기
        //        yut.NetworkHide(client.Key);
        //        //Debug.Log(client.Key + " 번 플레이어 윷 감춤");
        //    }
        //}
        //Debug.Log("윷 감추기 끝");
    }

    [Rpc(SendTo.Server)]
    public void ShowYutRpc()
    {
        List<NetworkObject> yutList = new List<NetworkObject>();

        for (int i = 0; i < yutNum; i++)
        {
            Yut yut = yuts[i];
            yut.soundActivated = true;
            //yutList.Add(yut.GetComponent<NetworkObject>());
            //yut.gameObject.SetActive(true);
        }

        //foreach (var client in NetworkManager.Singleton.ConnectedClients)
        //{
        //    //서버는 패스
        //    if (client.Key == NetworkManager.ServerClientId) continue;

        //    foreach (var yut in yutList)
        //    {
        //        //이미 show 되어있으면 패스
        //        if (yut.IsNetworkVisibleTo(client.Key)) continue;

        //        //드러내기
        //        yut.NetworkShow(client.Key);
        //        //Debug.Log(client.Key + " 번 플레이어 윷 드러내기");
        //    }
        //}
        //Debug.Log("윷 드러내기 끝");
    }

    public override void OnNetworkDespawn()
    {
        //낙 트리거 해제
        outCollision.OnYutCollided -= YutFalled;
    }

    private void FixedUpdate()
    {
        //if (!IsOwner) return; //오너 아니면 리턴

        if (Input.GetKeyDown(KeyCode.R))
        {
            throwChance++;
            Debug.Log(throwChance);
            //YutResultCount();
        }

        //if (Input.GetKeyDown(KeyCode.A))
        //{
        //    if (!autoYut)
        //    {
        //        autoYut = true;
        //        throwChance = 100;
        //    }
        //    else
        //    {
        //        autoYut= false;
        //        throwChance = 0;
        //    }
        //    //YutResultCount();
        //}

        if (Input.GetKeyDown(KeyCode.Y))
        {
            int backdoCnt, doCnt, gaeCnt, gurCnt, yutCnt, moCnt;
            backdoCnt = doCnt = gaeCnt = gurCnt = yutCnt = moCnt = 0;

            foreach (var value in results)
            {
                var result = value.yutResult;

                switch (result)
                {
                    case YutResult.BackDo:
                        backdoCnt++;
                        break;
                    case YutResult.Do:
                        doCnt++;
                        break;
                    case YutResult.Gae:
                        gaeCnt++;
                        break;
                    case YutResult.Gur:
                        gurCnt++;
                        break;
                    case YutResult.Yut:
                        yutCnt++;
                        break;
                    case YutResult.Mo:
                        moCnt++;
                        break;
                }
            }

            Debug.Log("백도 : " + backdoCnt);
            Debug.Log("도 : " + doCnt);
            Debug.Log("개 : " + gaeCnt);
            Debug.Log("걸 : " + gurCnt);
            Debug.Log("윷 : " + yutCnt);
            Debug.Log("모 : " + moCnt);
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

            //던진 당사자고, 타임아웃되면 알아서 던짐
            if (isThrower && Time.time - powerStartTime > powerTimeOut)
            {
                ThrowButtonReleased();
            }
        }
    }

    public void ThrowButtonPressed()
    {
        if (isCalulating)
        {
            //버튼 못누른다고 안내
            GameManager.Instance.announceCanvas.ShowAnnounceText("결과 대기 중!", 2f);
            return;
        }
        //지금 누구 턴인지
        //if ((ulong)GameManager.Instance.mainGameProgress.currentPlayerNumber != NetworkManager.LocalClientId)
        //{
        //    return;
        //}
        //던질 기회가 남았는지
        if (throwChance < 1)
        {
            Debug.Log("던질 기회 없음");
            GameManager.Instance.announceCanvas.ShowAnnounceText("기회 없음!");
            return;
        }

        Debug.Log("버튼 프레스 아이디 : " + NetworkManager.Singleton.LocalClientId);

        //누르고 있는 동안 파워 게이지 작동
        powerAmount = 0;
        powerStartTime = Time.time;

        isThrower = true;
        isThrowButtonDown = true;
        CallPowerGaugeSyncRpc(NetworkManager.Singleton.LocalClientId, true, powerAmount);
    }

    [Rpc(SendTo.Server)]
    void CallPowerGaugeSyncRpc(ulong senderId, bool value, float power)
    {
        PowerGaugeSyncRpc(senderId, value, power);
    }

    [Rpc(SendTo.ClientsAndHost)]
    void PowerGaugeSyncRpc(ulong senderId, bool value, float power)
    {
        //콜한 당사자는 스킵
        if(senderId == NetworkManager.Singleton.LocalClientId) return;
        
        powerAmount = power;
        isThrowButtonDown = value;
        powerGauge.fillAmount = Mathf.Clamp(powerAmount, 0, 1);
    }

    public void ThrowButtonReleased()
    {
        //버튼 풀려있으면 작동 안되게함
        if (!isThrowButtonDown) return;

        Debug.Log("버튼 릴리즈 아이디 : " + NetworkManager.Singleton.LocalClientId);

        //버튼 풀면 파워게이지 멈추고
        isThrowButtonDown = false;
        CallPowerGaugeSyncRpc(NetworkManager.Singleton.LocalClientId, false, powerAmount);

        isThrower = false;

        //윷 몇개 던질지 확인하고, 현재 파워로 던짐
        ThrowYutsServerRpc(yutNum, Mathf.Clamp(minThrowPower + (maxThrowPower - minThrowPower) * powerAmount, minThrowPower, maxThrowPower), new ServerRpcParams());
        throwChance--;
        //Debug.Log("던짐");
    }

    [Rpc(SendTo.ClientsAndHost)]
    void StartYutCalculatingRpc()
    {
        isCalulating = true;
    }

    [ServerRpc(RequireOwnership = false)]
    void ThrowYutsServerRpc(int yutNums, float power, ServerRpcParams rpcParams)
    {
        //isCalulating = true;
        StartYutCalculatingRpc();
        //윷 보이게 하기
        //ShowYutRpc();

        //Debug.Log("던지는 파워 : " + power);

        for (int i = 0; i < yutNums; i++)
        {
            Yut yut = yuts[i];

            //윷을 원래 위치로 돌리기
            yut.transform.localPosition = yut.originPos; //외않됢?
            yut.transform.localRotation = yut.originRot; //외않됢? 이제 됢!

            //윷 던지기
            //던지기 전에 움직임을 없애고(이상한 방향으로 날라가는거 방지)
            yut.Rigidbody.linearVelocity = Vector3.zero;
            yut.Rigidbody.angularVelocity = Vector3.zero;

            //던질때는 캐릭터랑 안부딫히게 잠깐 콜라이더 껐다 켜고
            yut.AllColliderDeactivate();
            StartCoroutine(YutCollisionOn(yut, 0.1f));

            //윷에 힘을 가해 위쪽 방향으로 던지고, 랜덤한 토크를 가해 앞 뒷면을 조절한다
            float randomSign = Mathf.Sign(Random.Range(-1, 1));
            float randomTorque = Random.Range(minTorque, maxTorque); //최소한 한바퀴 이상 돌 수 있게 토크 조절
            yut.Rigidbody.AddForce(Vector3.up * power, ForceMode.Impulse); //250 ~ 300
            //yut.Rigidbody.AddTorque(Random.insideUnitSphere.normalized * 15 * randomSign, ForceMode.Impulse); //6~
            yut.Rigidbody.AddTorque(yut.transform.forward * randomTorque * randomSign, ForceMode.Impulse);

            float randomOtherTorgue = Random.Range(-2f, 2f);
            yut.Rigidbody.AddTorque(yut.transform.up * randomOtherTorgue, ForceMode.Impulse);
            yut.Rigidbody.AddTorque(yut.transform.right * randomOtherTorgue, ForceMode.Impulse);

            yut.torqueSign = randomSign; //토크 부호 저장
            yut.soundActivated = true; //사운드 켬
            //yut.Rigidbody.excludeLayers = LayerMask.GetMask("Player");
        }
        
        StartCoroutine(YutResultCheck(0, yutNums, rpcParams));
    }

    float GetRandomSign()
    {
        return Mathf.Sign(Random.Range(-1, 1));
    }

    IEnumerator YutCollisionOn(Yut yut, float second)
    {
        yield return new WaitForSecondsRealtime(second);

        yut.AllColliderActivate(); 
    }

    IEnumerator YutResultCheck(float timePassed, int yutNums, ServerRpcParams rpcParams)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;

        bool yutStable = false;
        bool yutStabledPrev = false;
        isYutFalled = false;
        YutFace[] faces = new YutFace[yutNums];

        //일정 시간동안 반복
        //waitTime 안에 결과가 안나오면 에러남
        while (timePassed < waitTime)
        {
            //waitInterval 마다 윷 상태를 확인
            yield return new WaitForSecondsRealtime(waitInterval);
            
            if (isYutFalled)
            {
                Debug.Log("낙으로 루프 탈출");
                break;
            }

            for (int i = 0; i < yutNums; i++)
            {
                Yut yut = yuts[i];

                //윷이 멈춰있으면 결과 확인 가능한걸로 판단 -> 완전히 안멈추면 결과 안나옴
                //윷이 수직으로 서있을때 레이캐스트 해버리는 상황 -> 앞 뒷면 레이캐스트를 쏴서 바닥에 면이 붙어있는지 체크
                //잠깐만 멈춰있어도 타이밍 겹치면 완전히 멈춰버린걸로 판정해버림 -> 다음 루프에서도 멈춰있는지 체크
                //Debug.Log("속도 : " + yut.Rigidbody.linearVelocity.magnitude + " 각속도 : " + yut.Rigidbody.angularVelocity.magnitude);
                //Debug.Log("리지드바디 잠? " + yut.Rigidbody.IsSleeping());
                if (yut.Rigidbody.linearVelocity.magnitude < 0.5f && yut.Rigidbody.angularVelocity.magnitude < 0.5f)
                {
                    //다 멈추면 true로 유지
                    yutStable = true;

                    //찔끔씩 움직이는거 방지하기 위해서 키네마틱 잠깐 껐다 킴
                    //yut.Rigidbody.isKinematic = true;
                    //yut.Rigidbody.isKinematic = false;

                    //Debug.Log("윷 멈춤");
                    //Debug.Log(i + "번 윷 앞뒷면 : " + faces[i]);
                    //Debug.Log("윷 서있음? " + yut.IsVertical);
                }
                else
                {
                    //하나라도 안멈춰있으면 루프 지속
                    yutStable = false;
                    break;
                }

                //윷 서있으면 안정적이지 않다고 판정, 루프 지속
                if (yut.IsVertical)
                {
                    yutStable = false;
                    break;
                }
            }

            //이전 루프때도 멈춰있었는지 판별
            if (yutStable)
            {
                if (!yutStabledPrev)
                {
                    yutStabledPrev = true;
                    yutStable = false;
                }
            }
            else
            {
                yutStabledPrev = false;
            }

            //Debug.Log("전에 멈춤 ? : " +  yutStabledPrev + " 이번에 멈춤? : " + yutStable);

            //이번 루프와 이전 루프 모두 멈춰있었으면 완전히 멈춘걸로 판단
            if (yutStable && yutStabledPrev)
            {
                //쓰는거 초기화
                faceStable = false;
                backDo = false;
                faceDown = 0;
                isFaceError = false;

                for (int i = 0; i < yutNums; i++)
                {
                    YutFace curFace = CalcYutResult(yuts[i]);
                    
                    //이전 결과랑 이번 결과랑 다르면 안정적이지 않다고 판단, 다시 계산
                    //Debug.Log(i + " 이전 결과 : " + faces[i] + " 현재 결과 : " + curFace);
                    if (faces[i] == curFace)
                    {
                        faceStable = true;
                    }
                    else
                    {
                        faceStable = false;
                    }

                    //면이 에러면 다시 계산
                    if (curFace == YutFace.Error)
                    {
                        isFaceError = true;
                        faceStable = false;
                    }

                    //레이캐스트 해서 앞뒷면 계산
                    faces[i] = curFace;

                    //결과 다르면 최종결과 안나옴
                    if (!faceStable) break;

                    //Debug.Log(i + "번 최종 결과 : " + faces[i]);

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

                //면 안정적이면 루프 탈출, 결과 냄
                if (faceStable || isYutFalled)
                {
                    break;
                }
                else //faceStable이 false지만 타임아웃으로 루프 탈출했을때 결과가 나와버리는 버그 방지
                {
                    isFaceError = true;
                }
            }

            timePassed += waitInterval;
        }

        if (isYutFalled)
        {
            Debug.Log("낙! 턴이 넘어갑니다");

            //윷 결과 비워버리고
            //낙 판정하는 부울 초기화
            //던지는 횟수 초기화
            ClearYutResuliClientRpc();
            //YutFalledRpc(false);
            GameManager.Instance.announceCanvas.ShowAnnounceTextClientRpc("낙! 턴이 넘어갑니다!");
            ThrowChanceChangeClientRpc(-999, senderId);
            EndYutCalculatingRpc();

            //이동 끝 실행해서 턴 넘김
            YutFallTurnEndRpc(RpcTarget.Single(senderId, RpcTargetUse.Temp));
            yield break;
        }

        //Debug.Log("총 개수 : " + faceDown);
        if (!yutStable)
        {
            Debug.Log("결과 산출 실패 : 타임아웃");
            GameManager.Instance.announceCanvas.ShowAnnounceTextClientRpc("시간 초과, 다시 던지세요!");
            //타임아웃나면 다시 던질 수 있게 기회 더 줌
            ThrowChanceChangeClientRpc(1, senderId);

            EndYutCalculatingRpc();
            yield break;
        }

        if (isFaceError)
        {
            Debug.Log("결과 산출 실패 : 면 판단 실패");
            GameManager.Instance.announceCanvas.ShowAnnounceTextClientRpc("결과 오류, 다시 던지세요!");
            ThrowChanceChangeClientRpc(1, senderId);

            EndYutCalculatingRpc();
            yield break;
        }

        switch (faceDown)
        {
            case 0:
                AddYutResultClientRpc(YutResult.Mo, senderId);
                ThrowChanceChangeClientRpc(1, senderId);
                GameManager.Instance.announceCanvas.ShowAnnounceTextClientRpc("모!");
                break;
            case 1:
                if (backDo)
                {
                    AddYutResultClientRpc(YutResult.BackDo, senderId);
                GameManager.Instance.announceCanvas.ShowAnnounceTextClientRpc("백도!");
                    break;
                }
                AddYutResultClientRpc(YutResult.Do, senderId);
                GameManager.Instance.announceCanvas.ShowAnnounceTextClientRpc("도!");
                break;
            case 2:
                AddYutResultClientRpc(YutResult.Gae, senderId);
                GameManager.Instance.announceCanvas.ShowAnnounceTextClientRpc("개!");
                break;
            case 3:
                AddYutResultClientRpc(YutResult.Gur, senderId);
                GameManager.Instance.announceCanvas.ShowAnnounceTextClientRpc("걸!");
                break;
            case 4:
                AddYutResultClientRpc(YutResult.Yut, senderId);
                ThrowChanceChangeClientRpc(1, senderId);
                GameManager.Instance.announceCanvas.ShowAnnounceTextClientRpc("윷!");
                break;
            default:
                AddYutResultClientRpc(YutResult.Error, senderId);
                break;
        }

        EndYutCalculatingRpc();

        //null 리턴하면 코루틴이 안멈추나? break랑 다른건?
        yield break;
    }

    [Rpc(SendTo.ClientsAndHost)]
    void EndYutCalculatingRpc()
    {
        isCalulating = false;
        isYutFalled = false;
        powerGauge.fillAmount = 1f;

        if (autoYut)
        {
            CallYutThrowRpc();
        }
    }

    [Rpc(SendTo.Server)]
    void CallYutThrowRpc()
    {
        ThrowYutsServerRpc(4, Random.Range(minThrowPower, maxThrowPower), new ServerRpcParams());
        throwChance--;
    }

    [Rpc(SendTo.SpecifiedInParams)]
    void YutFallTurnEndRpc(RpcParams rpcParams)
    {
        GameManager.Instance.mainGameProgress.EndMove(); //해당 클라이언트에서 실행해야함
    }

    YutFace CalcYutResult(Yut yut)
    {
        bool isFront = YutRayCast(yut, true);
        bool isBack = YutRayCast(yut, false);
        //Debug.Log("앞면 : " +  isFront + " 뒷면 : " +  isBack);

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

    [ClientRpc]
    void AddYutResultClientRpc(YutResult result, ulong senderId)
    {
        //Debug.Log("로컬 클라이언트 id : " + NetworkManager.Singleton.LocalClientId + "\nrpc요청 id : " + senderId + "\n오너 클라이언트 id : " + OwnerClientId);

        //윷 던지는거 요청한 클라이언트의 윷 결과창을 갱신
        if (senderId == NetworkManager.Singleton.LocalClientId)
        {
            Debug.Log("윷 결과 스폰 요청");

            
            //SpawnYutResultServerRpc(result, senderId);
            //SetViewportTransform();
        }
        
        var yutResult = Instantiate(yutResultPrefab, yutResultContent.transform);
        yutResult.SetYutText(result);
        yutResult.SetClientId(senderId);
        results.Add(yutResult);
    }

    [Rpc(SendTo.Server)]
    public void CallRemoveResultRpc(YutResult result, ulong except)
    {
        RemoveResultRpc(result, except);
    }

    [Rpc(SendTo.ClientsAndHost)]
    void RemoveResultRpc(YutResult result, ulong except)
    {
        //제외 대상(리무브 요청한 클라이언트)은 리무브 안함
        if(except == NetworkManager.Singleton.LocalClientId) return;
        
        RemoveYutResult(result);
    }

    //리스트에서 윷 결과 삭제
    public void RemoveYutResult(YutResult result)
    {
        //Debug.Log("id : " + NetworkManager.Singleton.LocalClientId + "" + result + "삭제");
        //results.Remove(result);
        //SetViewportTransform();
        foreach(var value in results)
        {
            if(value.yutResult == result)
            {
                Debug.Log("일치하는 윷 결과 찾음, 디스트로이");

                results.Remove(value);
                Destroy(value.gameObject);
                return;
            }
        }

        Debug.Log("일치하는 윷 결과 못찾음");
    }

    [ServerRpc(RequireOwnership = false)]
    void SpawnYutResultServerRpc(YutResult result, ulong senderId)
    {
        Debug.Log("윷 결과 스폰");
        var yutResult = Instantiate(yutResultPrefab);
        yutResult.SetYutText(result);
        var no = yutResult.GetComponent<NetworkObject>();
        no.SpawnWithOwnership(senderId);

        if (no.TrySetParent(yutResultContent.transform))
        {
            Debug.Log("부모 설정 성공");
        }
        else
        {
            Debug.Log("부모 설정 실패");
        }

    }

    //윷 결과를 중앙에 정렬하기 위한 함수
    void SetViewportTransform()
    {
        float positionX = 295;
        float scaleW = 485;

        //1개 295,  2개 205, 3개 115, 4개 25, 5개이상 0
        if (results.Count <= 1)
        {
            //원점
            positionX = 295;
            scaleW = 485;
        }
        else if(results.Count > 1)
        {
            //리스트 개수따라 위치 조절
            positionX = 295 - (results.Count - 1) * 90;
            scaleW = 485 + (results.Count - 1) * 90;
        }
        else if(results.Count > 4)
        {
            //0으로 고정
            positionX = 0;
            scaleW = 780;
        }
        else
        {
            //오류?
            Debug.Log("뷰포트 위치 계산 실패 : 예상되지 않은 케이스");
        }
        Debug.Log("스케일 : " + scaleW);
        viewport.anchoredPosition = new Vector3(Mathf.Clamp(positionX, 0, 295), viewport.anchoredPosition.y);
        viewport.sizeDelta.Set(Mathf.Clamp(scaleW, 485, 780), viewport.sizeDelta.y);
    }

    [ClientRpc]
    public void ClearYutResuliClientRpc()
    {
        Debug.Log("윷 결과 리스트 클리어 요청");
        //모든 클라이언트에서 리스트 클리어
        foreach (var result in results)
        {
            Debug.Log("없애줘 : " + result);
            result.DestroyYutResult();
        }

        results.Clear();
    }

    [ClientRpc]
    public void ThrowChanceChangeClientRpc(int num, ulong senderId)
    {
        //해당 클라이언트의 윷 던지기 횟수를 갱신
        if(senderId == NetworkManager.Singleton.LocalClientId)
        {
            //-99이하면 턴 0으로 초기화
            if(num <= -99)
            {
                throwChance = 0;
                return;
            }
            throwChance += num;
        }
    }
    public void SpawnCharacter()
    {
        PlayerManager.Instance.SpawnCharacter();
    }
    private void OnThrowChangeChanged()
    {
        throwChanceTmp.text = "기회:"+throwChance.ToString();
    }

    //낙 판정 관련
    void YutFalled(int num)
    {
        //4개 다 떨어지면 좋은 아이템 지급??

        if (num > 0)
        {
            isYutFalled = true;
        }
        else
        {
            isYutFalled = false;
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void YutFalledRpc(bool value)
    {
        isYutFalled = value;
    }

}
