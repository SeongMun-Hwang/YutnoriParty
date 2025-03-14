using UnityEngine;

public class MouseReactivePosition : MonoBehaviour
{
    RectTransform rectTransform;
    public float moveSpeed = 5f; // 이동 속도 조절
    private Vector3 targetPosition;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        targetPosition = rectTransform.anchoredPosition;
    }

    void Update()
    {
        Vector3 mousePosition = Input.mousePosition;
        Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);
        Vector3 offset = mousePosition - screenCenter;
        float movePosX = -offset.y * moveSpeed * 0.001f;
        float movePosY = offset.x * moveSpeed * 0.001f;

        targetPosition = new Vector3(movePosX, movePosY, 0f);
        rectTransform.anchoredPosition = Vector3.Lerp(rectTransform.anchoredPosition, targetPosition, Time.deltaTime * 2f);
    }
}
