using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class StarCrown : MonoBehaviour
{
    [SerializeField] List<GameObject> stars;
    public NetworkVariable<ulong> ownerId = new NetworkVariable<ulong>(99);

    public float rotationSpeed = 10f;

    private void FixedUpdate()
    {
        transform.Rotate(Vector3.up * rotationSpeed * Time.fixedDeltaTime);
    }
}
