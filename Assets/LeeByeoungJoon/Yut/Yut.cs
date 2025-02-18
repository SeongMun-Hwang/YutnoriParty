using System;
using UnityEngine;

public class Yut : MonoBehaviour
{
    [HideInInspector] public Transform origin;

    Rigidbody rigidbody;
    [SerializeField] GameObject yutModel;

    public GameObject GameObject { get { return gameObject; } }
    public Rigidbody Rigidbody { get { return rigidbody; } }

    public Action<bool> OnFaceDown;
    public Action<bool> OnGround;
    public Action<bool> OnMoveStop;
    bool onGround = false;
    bool onFaceDown;
    bool throwed = false;

    private void Awake()
    {
        rigidbody = GetComponent<Rigidbody>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.tag == "Ground")
        {
            onGround = true;
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        //�������� �ƴϸ� �н�
        if (!throwed) return;

        //����������, ���� ���
        if (onGround)
        {
            //�������� ������ ��������
            if(rigidbody.linearVelocity == Vector3.zero && rigidbody.angularVelocity == Vector3.zero)
            {
                //�� �޸��� �Ǻ��� �̺�Ʈ�� �߻���Ų��
                //OnFaceDown?.Invoke(CalcYutResult());
            }
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.tag == "Ground")
        {
            onGround = false;
        }
    }
    public void ThrowYut(float throwPower, float torque)
    {
        throwed = true;
        
    }

    
}
