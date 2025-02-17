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
        //던져진거 아니면 패스
        if (!throwed) return;

        //던져졌으면, 땅에 닿고
        if (onGround)
        {
            //움직임이 완전히 멈췄을때
            if(rigidbody.linearVelocity == Vector3.zero && rigidbody.angularVelocity == Vector3.zero)
            {
                //앞 뒷면을 판별해 이벤트를 발생시킨다
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
