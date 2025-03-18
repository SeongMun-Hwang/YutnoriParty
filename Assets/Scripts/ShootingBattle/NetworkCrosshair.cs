using Unity.Netcode;
using UnityEngine;

public class NetworkCrosshair : NetworkBehaviour
{
    public Camera mainCamera;
    public NetworkVariable<Color32> networkColor = new NetworkVariable<Color32>(
        new Color32(255, 255, 255, 255),
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );
    public SpriteRenderer spriteRenderer;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        spriteRenderer = GetComponent<SpriteRenderer>();
        // 색상 변경 이벤트
        networkColor.OnValueChanged += (oldColor, newColor) =>
        {
            spriteRenderer.color = newColor;
        };

        spriteRenderer.color = networkColor.Value;
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
        if (IsOwner)
        {
            if (mainCamera == null || !mainCamera.gameObject.activeSelf)
            {
                SetCamera();
                return;
            }

            Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0;
            transform.position = mousePos;
            // Debug.Log($"{NetworkManager.Singleton.LocalClientId} 위치 업뎃 {transform.position}");
        }
    }

    [ServerRpc]
    public void SetColorServerRpc(Color32 newColor)
    {
        if (!IsOwner) return;
        networkColor.Value = newColor; // 네트워크 변수 업데이트 → 자동 동기화
    }
}