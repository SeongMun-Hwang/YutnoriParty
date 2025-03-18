using System.Collections.Generic;
using UnityEngine;

public class StarCrown : MonoBehaviour
{
    [SerializeField] List<GameObject> stars;
    public float rotationSpeed = 10f;

    private void FixedUpdate()
    {
        transform.Rotate(Vector3.up * rotationSpeed * Time.fixedDeltaTime);
    }
}
