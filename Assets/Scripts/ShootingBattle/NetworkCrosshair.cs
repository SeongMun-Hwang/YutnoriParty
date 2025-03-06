using Unity.Netcode;
using UnityEngine;

public class NetworkCrosshair : NetworkBehaviour
{
    public Camera mainCamera;

    private void Start()
    {
        SetCamera();
    }

    private void SetCamera()
    {
        if (IsOwner) // 본인만 실행
        {
            mainCamera = Camera.main;
        }
    }

    private void Update()
    {
        if (!IsOwner || mainCamera == null)
        {
            SetCamera();
            return;
        }

        Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;
        transform.position = mousePos;

        Debug.Log($"{NetworkManager.Singleton.LocalClientId} 위치 업뎃 {transform.position}");
    }
}