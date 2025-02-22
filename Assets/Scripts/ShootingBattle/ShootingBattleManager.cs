using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class ShootingBattleManager : NetworkBehaviour
{
    [SerializeField] GameObject StarPrefab;
    private bool isPlaying;
    [SerializeField] private float spawnDuration = 1.5f;
    [SerializeField] private TMP_Text timerUI;
    [SerializeField] private float timer = 15f;

    private void Start()
    {
        isPlaying = true;
        StartCoroutine(SpawnStar());
        StartCoroutine(CountTimer());
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

    private IEnumerator CountTimer()
    {
        while (isPlaying)
        {
            yield return null;
            timer--;
            timerUI.text = timer.ToString();
            yield return new WaitForSecondsRealtime(1f);

            if (timer == 0)
            {
                isPlaying = false;
                yield break;
            }
        }
    }

    private IEnumerator SpawnStar()
    {
        while (isPlaying)
        {
            yield return null;

            Vector3 randomPos = new Vector3(Random.Range(-7f, 7f), Random.Range(-2f, 2f), 0);
            Instantiate(StarPrefab, randomPos, transform.rotation);

            yield return new WaitForSecondsRealtime(spawnDuration);
            spawnDuration = Mathf.Clamp(spawnDuration - 0.4f, 0.2f, 1.5f);
        }
    }
}
