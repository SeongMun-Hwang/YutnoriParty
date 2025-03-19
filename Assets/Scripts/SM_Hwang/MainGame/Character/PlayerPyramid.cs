using TMPro;
using Unity.Netcode;
using UnityEngine;

public class PlayerPyramid : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI playerIndex;
    private float rotationSpeed = 10f;
    private float moveSpeed=0.1f;
    private float yRange = 0.1f;

    private void FixedUpdate()
    {
        transform.Rotate(Vector3.forward * rotationSpeed * Time.fixedDeltaTime);

        float yAxis = Mathf.PingPong(Time.time * moveSpeed, yRange * 2) - yRange;
        transform.position = new Vector3(transform.position.x, 2 + yAxis, transform.position.z);
    }
}