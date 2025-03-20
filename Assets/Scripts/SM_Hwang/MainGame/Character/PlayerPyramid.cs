using TMPro;
using Unity.Netcode;
using UnityEngine;

public class PlayerPyramid : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI playerIndex;
    private float rotationSpeed = 10f;
    private float moveSpeed=0.1f;
    private float yRange = 0.1f;
    private float initialY = 3f;
    public float overlappedHeight = 0f;
    private void FixedUpdate()
    {
        transform.Rotate(Vector3.forward * rotationSpeed * Time.fixedDeltaTime);
       // playerIndex.gameObject.transform.rotation = Quaternion.identity;

        float yAxis = Mathf.PingPong(Time.time * moveSpeed, yRange * 2) - yRange;
        transform.position = new Vector3(transform.position.x, initialY+overlappedHeight+yAxis, transform.position.z);
    }
    public void SetUserName(string name)
    {
        playerIndex.text = name;
    }
}