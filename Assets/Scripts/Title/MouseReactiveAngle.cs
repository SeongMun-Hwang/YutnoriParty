using UnityEngine;

public class MouseReactiveAngle : MonoBehaviour
{
    public float rotationSpeed = 5f; // 회전 속도 조절
    private Quaternion targetRotation;

    void Start()
    {
        targetRotation = transform.rotation;
    }

    void Update()
    {
        Vector3 mousePosition = Input.mousePosition;
        Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);
        Vector3 offset = mousePosition - screenCenter;
        float rotationX = -offset.y * rotationSpeed * 0.01f;
        float rotationY = offset.x * rotationSpeed * 0.01f;

        targetRotation = Quaternion.Euler(rotationX, rotationY, 0f);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * 2f);
    }
}
