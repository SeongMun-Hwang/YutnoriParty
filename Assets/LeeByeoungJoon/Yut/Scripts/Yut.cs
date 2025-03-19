using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Yut : NetworkBehaviour
{
    [HideInInspector] public Vector3 originPos;
    [HideInInspector] public Quaternion originRot;
    float torque = 1f;
    float smallSoundPower = 2f;
    float middleSoundPower = 7f;
    float largeSoundPower = 12f;

    public float torqueSign = 0;

    //[SerializeField] new Collider collider;
    [SerializeField] List<Collider> colliders;
    //public Collider Collider { get { return collider; } }

    [SerializeField] PhysicsMaterial yutPhysicsMaterial;
    [SerializeField] PhysicsMaterial noFrictionMetarial;

    new Rigidbody rigidbody;
    public Rigidbody Rigidbody { get { return rigidbody; } }

    float gravity = 9.8f;
    float gravityFactor = 1;
    float characterBounce = 1f;
    //bool isGrounded = false;
    bool isVertical = false;
    public bool IsVertical { get { return isVertical; } }
    
    private bool _soundActivated = false;
    public bool soundActivated
    {
        get { return _soundActivated; }
        set
        {
            Debug.Log($"soundActivated 값 변경 : {_soundActivated} -> {value}");
            _soundActivated = value;
        }
    }


    private void Awake()
    {
        rigidbody = GetComponent<Rigidbody>();
    }

    //답답하게 떨어져서 좀 빨리 떨어지게 함
    private void FixedUpdate()
    {
        //윷 움직임은 서버에서만 관리
        if(!IsServer) return;

        rigidbody.AddForce(Vector3.down * gravity * gravityFactor, ForceMode.Impulse);
    }

    private void OnCollisionEnter(Collision collision)
    {
        //플레이어랑 닿으면 반발력 가함
        if(collision.gameObject.tag == "Player")
        {
            foreach(ContactPoint contact in collision.contacts)
            {
                //Debug.DrawRay(contact.point, contact.normal, Color.yellow);
                rigidbody.AddForce(contact.normal * characterBounce, ForceMode.Impulse);
            }
        }

        //사운드 활성화 안되어있으면 소리 안나게
        if (!soundActivated) return;

        //Debug.Log("소리 날거임");

        if(collision.gameObject.tag != "Player") //플레이어는 말랑하니까 나무소리 안남
        {
            //Debug.Log("플레이어 아님");

            float collisionPower = collision.relativeVelocity.magnitude;
            //Debug.Log("힘: " + collisionPower);
            if (collisionPower > smallSoundPower)
            {
                //Debug.Log("소리 조건 충족");

                PlayYutSoundRpc(collisionPower);
            }
        }
        
        //isGrounded = true;
    }

    [Rpc(SendTo.ClientsAndHost)]
    void PlayYutSoundRpc(float power)
    {
        if(power > largeSoundPower)
        {
            AudioManager.instance.Playsfx(20);
        }
        else if(power > middleSoundPower)
        {
            AudioManager.instance.Playsfx(21);
        }
        else //제일 작은 소리
        {
            AudioManager.instance.Playsfx(19);
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        //isGrounded = false;
    }

    private void OnTriggerStay(Collider other)
    {
        if(other.tag == "Ground")
        {
            //Debug.Log("섰음!!!!!!!");
            //수직으로 섰으니까 토크 가함
            rigidbody.AddTorque(transform.forward * torque * torqueSign, ForceMode.Impulse);
            //마찰력 없는 피직스 머티리얼로 교체
            //if (collider.material != noFrictionMetarial)
            //{
            //    collider.material = noFrictionMetarial;
            //}
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
            //마찰력 있는 기본 피직스 머티리얼로 교체
            //collider.material = yutPhysicsMaterial;
        }
    }

    public void AllColliderActivate()
    {
        foreach(var col in colliders)
        {
            col.isTrigger = false;
        }
    }

    public void AllColliderDeactivate()
    {
        foreach (var col in colliders)
        {
            col.isTrigger = true;
        }
    }
}
