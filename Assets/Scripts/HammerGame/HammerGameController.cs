using Unity.Netcode;
using UnityEngine;

public class HammerGameController : NetworkManager
{
    public float moveSpeed = 10f;
    private bool isAttacked = false;
    private Animator animator;
    private Rigidbody rb;

    void Start()
    {
        animator=GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        
    }
}
