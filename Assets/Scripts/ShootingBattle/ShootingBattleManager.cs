using Unity.Netcode;
using UnityEngine;

public class ShootingBattleManager : NetworkBehaviour
{
    [SerializeField] GameObject StarPrefab;
    public int maxStar = 10;

    private void Start()
    {
        for (int i = 0; i < maxStar; i++)
        {
            Vector3 randomPos = new Vector3(Random.Range(-5f, 5f), Random.Range(-3f, 3f), 0);
            Instantiate(StarPrefab, randomPos, transform.rotation);
        }
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
