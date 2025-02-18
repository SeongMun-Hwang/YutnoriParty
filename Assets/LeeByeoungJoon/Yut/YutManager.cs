using System.Collections;
using System.Collections.Generic;
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
public class YutManager : MonoBehaviour
{
    [SerializeField] Yut yut;
    [SerializeField] YutPlate yutPlate;

    List<Yut> yuts;
    List<YutResult> results = new List<YutResult>();

    LayerMask ground;
    public LayerMask Ground { get { return ground; } }

    int faceDown = 0;
    int yutGrounded = 0;
    float throwPower = 10;
    float torque = 3;
    float yutSpacing = 2;
    float waitTime = 5;
    float waitInterval = 1;
    bool backDo = false;

    public int yutNum = 4;

    //�̱��� �ƴ�
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
        yuts = new List<Yut>();
        for(int i=0; i<yutNum; i++)
        {
            yuts.Add(Instantiate(yut));
            if(i > 0)
            {
                yuts[i].transform.position = yuts[i-1].transform.position + new Vector3(0, 0, yutSpacing);
            }
            
            yuts[i].origin = yuts[i].transform;
            yuts[i].origin.position = yuts[i].transform.position;
            yuts[i].origin.rotation = yuts[i].transform.rotation;
            //yuts[i].gameObject.SetActive(false);

        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            foreach (var result in results)
            {
                Debug.Log(result);
            }
        }
    }

    public void ThrowButtonPressed()
    {
        ThrowYuts(4);
        Debug.Log("����");
    }

    void ThrowYuts(int yutNums)
    {
        backDo = false;
        faceDown = 0;
        yutGrounded = 0;
        for(int i = 0; i < yutNums; i++)
        {
            Yut yut = yuts[i];
            //���� ���� ��ġ�� ������
            yut.transform.localPosition = yut.origin.position; //�ܾʉ�?
            //yut.gameObject.SetActive(true);

            //�� ������
            //���� ���� ���� ���� �������� ������, ������ ��ũ�� ���� �� �޸��� �����Ѵ�
            yut.Rigidbody.AddForce(Vector3.up * throwPower, ForceMode.Impulse);
            yut.Rigidbody.AddTorque(transform.forward * Random.Range(-torque, torque), ForceMode.Impulse);
        }

        StartCoroutine(YutResultCheck(0, yutNums));
    }

    IEnumerator YutResultCheck(float timePassed, int yutNums)
    {
        bool yutStable = false;

        //���� �ð����� �ݺ�
        while (timePassed < waitTime)
        {
            //1�ʸ��� �� ���¸� Ȯ��
            yield return new WaitForSecondsRealtime(waitInterval);

            for(int i = 0; i < yutNums; i++)
            {
                Yut yut = yuts[i];

                //���� ���������� ��� Ȯ�� �����Ѱɷ� �Ǵ�
                if(yut.Rigidbody.linearVelocity == Vector3.zero && yut.Rigidbody.angularVelocity == Vector3.zero)
                {
                    //�� ���߸� true�� ����
                    yutStable = true;
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
                    }
                }
                else
                {
                    //�ϳ��� �ȸ��������� ���� ����
                    yutStable = false;
                }
            }

            if (yutStable)
            {
                break;
            }
            timePassed += waitInterval;
        }

        switch (faceDown)
        {
            case 0:
                results.Add(YutResult.Mo);
                break;
            case 1:
                if (backDo)
                {
                    results.Add(YutResult.BackDo);
                    break;
                }
                results.Add(YutResult.Do);
                break;
            case 2:
                results.Add(YutResult.Gae);
                break;
            case 3:
                results.Add(YutResult.Gur);
                break;
            case 4:
                results.Add(YutResult.Yut);
                break;
            default:
                results.Add(YutResult.Error);
                break;
        }
    }

    bool CalcYutResult(Yut yut)
    {
        RaycastHit hit;
        if (Physics.Raycast(yut.transform.position, transform.up, out hit, 10, ground))
        {
            return true;
        }
        return false;
    }
}
