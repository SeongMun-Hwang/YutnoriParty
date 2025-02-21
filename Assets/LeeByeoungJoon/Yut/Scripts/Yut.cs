using Unity.Netcode;
using UnityEngine;

public class Yut : NetworkBehaviour
{
    [HideInInspector] public Vector3 originPos;
    [HideInInspector] public Quaternion originRot;

    new Rigidbody rigidbody;

    public GameObject GameObject { get { return gameObject; } }
    public Rigidbody Rigidbody { get { return rigidbody; } }

    private void Awake()
    {
        rigidbody = GetComponent<Rigidbody>();
    }
}
