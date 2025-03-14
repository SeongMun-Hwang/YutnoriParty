using System.Collections;
using UnityEngine;

public class SpinSelf3D : MonoBehaviour
{
    private Vector3 rotationDirection;
    private float rotateSpeed;

    private void Start()
    {
        rotationDirection = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
        rotateSpeed = Random.Range(1f, 5f);
    }

    private void Update()
    {
        transform.Rotate(rotationDirection * Time.deltaTime * rotateSpeed);
    }
}
