using Unity.Netcode;
using UnityEngine;

public class Yut : NetworkBehaviour
{
    [HideInInspector] public Vector3 originPos;
    [HideInInspector] public Quaternion originRot;
    
    new Rigidbody rigidbody;
    float torque;
    float gravity = 9.8f;
    float gravityFactor = 1;
    bool isGrounded = false;
    bool isVertical = false;
    public bool IsVertical { get { return isVertical; } }
    public GameObject GameObject { get { return gameObject; } }
    public Rigidbody Rigidbody { get { return rigidbody; } }

    private void Awake()
    {
        rigidbody = GetComponent<Rigidbody>();
        torque = YutManager.Instance.Torque;
    }

    //답답하게 떨어져서 좀 빨리 떨어지게 함
    private void FixedUpdate()
    {
        //윷 움직임은 서버에서만 관리
        if(!IsServer) return;

        if (!isGrounded)
        {
            //Debug.Log("내려가");
            //중력 gravityFactor배
            rigidbody.AddForce(Vector3.down * gravity * gravityFactor, ForceMode.Impulse);
        }   
    }

    private void OnCollisionEnter(Collision collision)
    {
        isGrounded = true;
    }

    private void OnCollisionExit(Collision collision)
    {
        isGrounded = false;
    }

    private void OnTriggerStay(Collider other)
    {
        if(other.tag == "Ground")
        {
            //Debug.Log("섰음!!!!!!!");
            //수직으로 섰으니까 토크 가함
            //rigidbody.AddTorque(transform.forward * torque);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Ground")
        {
            isVertical = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Ground")
        {
            isVertical = false;
        }
    }
}
