using Unity.Netcode;
using UnityEngine;

public class ShootingBattleManager : NetworkBehaviour
{
    private void Start()
    {

    }

    public override void OnNetworkSpawn()
    {

    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0)) // 마우스 왼쪽 클릭
        {
            Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mousePosition, Vector2.zero);

            if (hit.collider != null)
            {
                // 클릭된 오브젝트의 특정 스크립트 실행
                ShootableStar star = hit.collider.GetComponent<ShootableStar>();
                if (star != null)
                {
                    star.OnClick();
                }
            }
        }
    }
}
