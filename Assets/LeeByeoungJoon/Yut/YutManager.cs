using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

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
public class YutManager : NetworkBehaviour
{
    [SerializeField] Yut yutPrefab;
    [SerializeField] GameObject yutResultContent;
    [SerializeField] YutResults yutResultPrefab;
    [SerializeField] Transform yutSpawnTransform;
    [SerializeField] LayerMask ground;

    List<Yut> yuts = new List<Yut>();
    List<YutResult> results = new List<YutResult>();

    int faceDown = 0;
    float throwPower = 10;
    float torque = 3;
    float yutSpacing = 2;
    float waitTime = 10;
    float waitInterval = 1;
    bool backDo = false;

    public int yutNum = 4;
    public int throwChance = 0;

    //�̱��� �ƴ�
    static YutManager instance;
    static public YutManager Instance
    {
        get
        {
            return instance;
        }
    }

    public override void OnNetworkSpawn()
    {
        //���������� �� ��ȯ
        if (!IsServer) return;

        Vector3 pos = yutSpawnTransform.position;

        for (int i = 0; i < yutNum; i++)
        {
            //�� ��ȯ�ϰ�
            yuts.Add(Instantiate(yutPrefab));
            
            Yut yut = yuts[i];
            //�� ��ü�� �߽� ��ġ ���߱� ���� �˲���
            yut.transform.position = pos + new Vector3(0, 0, -((yutNum - 1) * yutSpacing) / 2);
            yut.GetComponent<NetworkObject>().Spawn();

            //��ġ ����ְ�
            if (i > 0)
            {
                yut.transform.position = yuts[i - 1].transform.position + new Vector3(0, 0, yutSpacing);
            }

            //�ʱ�ȭ�� ��ġ ����
            yut.originPos = yut.transform.position;
            yut.originRot = yut.transform.rotation;

            //�Ⱥ��̰� �ϱ�
            //yut.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            MyTurn();
        }
    }

    void MyTurn()
    {
        throwChance = 1;
        Debug.Log("����, ���� ��ȸ +1");
    }

    public void ThrowButtonPressed()
    {
        //���� ���� ������
        //���� ��ȸ�� ���Ҵ���
        if (throwChance < 1)
        {
            Debug.Log("���� ��ȸ ����");
            return;
        }
        //�� � ������ Ȯ��
        ThrowYutsServerRpc(yutNum, new ServerRpcParams());
        throwChance--;
        Debug.Log("����");
    }

    [ServerRpc(RequireOwnership = false)]
    void ThrowYutsServerRpc(int yutNums, ServerRpcParams rpcParams)
    {
        backDo = false;
        faceDown = 0;
        for(int i = 0; i < yutNums; i++)
        {
            Yut yut = yuts[i];

            //���� ���� ��ġ�� ������
            yut.transform.localPosition = yut.originPos; //�ܾʉ�?
            yut.transform.localRotation = yut.originRot; //�ܾʉ�? ���� ��!

            //���̰� �ϱ�
            //yut.gameObject.SetActive(true);

            //�� ������
            //������ ���� �������� ���ְ�(�̻��� �������� ���󰡴°� ����)
            yut.Rigidbody.linearVelocity = Vector3.zero;
            yut.Rigidbody.angularVelocity = Vector3.zero;
            //���� ���� ���� ���� �������� ������, ������ ��ũ�� ���� �� �޸��� �����Ѵ�
            yut.Rigidbody.AddForce(Vector3.up * throwPower, ForceMode.Impulse);
            yut.Rigidbody.AddTorque(yut.transform.forward * Random.Range(-torque, torque), ForceMode.Impulse);
        }

        StartCoroutine(YutResultCheck(0, yutNums, rpcParams));
    }

    IEnumerator YutResultCheck(float timePassed, int yutNums, ServerRpcParams rpcParams)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;

        bool yutStable = false;

        //���� �ð����� �ݺ�
        //waitTime �ȿ� ����� �ȳ����� ������
        while (timePassed < waitTime)
        {
            //1�ʸ��� �� ���¸� Ȯ��
            yield return new WaitForSecondsRealtime(waitInterval);

            for(int i = 0; i < yutNums; i++)
            {
                Yut yut = yuts[i];

                //���� ���������� ��� Ȯ�� �����Ѱɷ� �Ǵ� -> ������ �ȸ��߸� ��� �ȳ���
                if(yut.Rigidbody.linearVelocity == Vector3.zero && yut.Rigidbody.angularVelocity == Vector3.zero)
                {
                    //�� ���߸� true�� ����
                    yutStable = true;
                }
                else
                {
                    //�ϳ��� �ȸ��������� ���� ����
                    yutStable = false;
                }
            }

            if (yutStable)
            {
                for(int i = 0; i<yutNums; i++)
                {
                    Yut yut = yuts[i];

                    //����ĳ��Ʈ �ؼ� �յ޸� ���
                    //�� ��� ���
                    if (CalcYutResult(yut))
                    {
                        //�鵵 ���
                        if (i == 0)
                        {
                            backDo = true;
                        }

                        faceDown++;
                        //Debug.Log("�޸� +1, �� ���� : " + faceDown);
                    }
                }
                break;
            }

            timePassed += waitInterval;
        }

        //Debug.Log("�� ���� : " + faceDown);
        if (!yutStable)
        {
            Debug.Log("��� ���� ���� : Ÿ�Ӿƿ�");
            //Ÿ�Ӿƿ����� �ٽ� ���� �� �ְ� ��ȸ �� ��
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

    bool CalcYutResult(Yut yut)
    {
        RaycastHit hit;
        Debug.DrawRay(yut.transform.position, yut.transform.right * 10, Color.red, 0.3f);
        if (Physics.Raycast(yut.transform.position, yut.transform.right, out hit, 10, ground))
        {
            //Debug.Log("�޸���");
            return true;
        }
        //Debug.Log("�ո���");
        return false;
    }

    [ClientRpc]
    void AddYutResultClientRpc(YutResult result, ulong senderId)
    {
        //Debug.Log("���� Ŭ���̾�Ʈ id : " + NetworkManager.Singleton.LocalClientId + "\nrpc��û id : " + senderId + "\n���� Ŭ���̾�Ʈ id : " + OwnerClientId);

        //�� �����°� ��û�� Ŭ���̾�Ʈ�� �� ���â�� ����
        if (senderId == NetworkManager.Singleton.LocalClientId)
        {
            results.Add(result);
            Instantiate(yutResultPrefab, yutResultContent.transform).SetYutText(result);
        }
    }

    [ClientRpc]
    void ThrowChanceChangeClientRpc(int num, ulong senderId)
    {
        //�ش� Ŭ���̾�Ʈ�� �� ������ Ƚ���� ����
        if(senderId == NetworkManager.Singleton.LocalClientId)
        {
            throwChance += num;
        }
    }
}
