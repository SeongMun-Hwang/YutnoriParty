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
    [SerializeField] Yut yutPrefab;
    [SerializeField] GameObject yutResultContent;
    [SerializeField] YutResults yutResultPrefab;
    [SerializeField] LayerMask ground;

    List<Yut> yuts;
    List<YutResult> results = new List<YutResult>();

    int faceDown = 0;
    float throwPower = 10;
    float torque = 3;
    float yutSpacing = 2;
    float waitTime = 10;
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
            yuts.Add(Instantiate(yutPrefab));
            if(i > 0)
            {
                yuts[i].transform.position = yuts[i-1].transform.position + new Vector3(0, 0, yutSpacing);
            }
            
            yuts[i].originPos = yuts[i].transform.position;
            yuts[i].originRot = yuts[i].transform.rotation;
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
        for(int i = 0; i < yutNums; i++)
        {
            Yut yut = yuts[i];
            //���� ���� ��ġ�� ������
            yut.transform.localPosition = yut.originPos; //�ܾʉ�?
            yut.transform.localRotation = yut.originRot; //�ܾʉ�? ���� ��!

            //yut.gameObject.SetActive(true);

            //�� ������
            //���� ���� ���� ���� �������� ������, ������ ��ũ�� ���� �� �޸��� �����Ѵ�
            yut.Rigidbody.AddForce(Vector3.up * throwPower, ForceMode.Impulse);
            yut.Rigidbody.AddTorque(yut.transform.forward * Random.Range(-torque, torque), ForceMode.Impulse);
        }

        StartCoroutine(YutResultCheck(0, yutNums));
    }

    IEnumerator YutResultCheck(float timePassed, int yutNums)
    {
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

                //���� ���������� ��� Ȯ�� �����Ѱɷ� �Ǵ�
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
            Debug.Log("��� ���� ����");
            yield break;
        }

        switch (faceDown)
        {
            case 0:
                //results.Add(YutResult.Mo);
                AddYutResult(YutResult.Mo);
                break;
            case 1:
                if (backDo)
                {
                    //results.Add(YutResult.BackDo);
                    AddYutResult(YutResult.BackDo);
                    break;
                }
                //results.Add(YutResult.Do);
                AddYutResult(YutResult.Do);
                break;
            case 2:
                //results.Add(YutResult.Gae);
                AddYutResult(YutResult.Gae);
                break;
            case 3:
                //results.Add(YutResult.Gur);
                AddYutResult(YutResult.Gur);
                break;
            case 4:
                //results.Add(YutResult.Yut);
                AddYutResult(YutResult.Yut);
                break;
            default:
                //results.Add(YutResult.Error);
                AddYutResult(YutResult.Error);
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

    void AddYutResult(YutResult result)
    {
        results.Add(result);
        yutResultPrefab.SetYutText(result.ToString());
        Instantiate(yutResultPrefab, yutResultContent.transform);
    }
}
