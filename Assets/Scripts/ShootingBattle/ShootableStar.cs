using System.Collections;
using Unity.Netcode;
using Unity.Services.Matchmaker.Models;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class ShootableStar : NetworkBehaviour
{
    private float rotationSpeed; // 회전 속도
    private Vector2 moveDirection; // 이동 방향
    private float moveSpeed; // 이동 속도
    private Vector2 minBounds, maxBounds; // 화면 바운더리
    private float boundsOffset = 24f;

    [SerializeField] private GameObject shotEffect;

    private Coroutine destroyCoroutine;
    public ShootingBattleManager manager;

    public override void OnNetworkSpawn()
    {
        while (minBounds == null && maxBounds == null)
        {
            Camera cam = Camera.main;
            minBounds = cam.ViewportToWorldPoint(new Vector3(0, 0, cam.nearClipPlane));
            maxBounds = cam.ViewportToWorldPoint(new Vector3(1, 1, cam.nearClipPlane));
        }

        InitPosition();
    }

    private void Update()
    {
        if (IsServer)
        {
            if (!manager.isPlaying.Value)
            {
                DestroyStarServerRpc(0);
            }
            else
            {
                transform.Rotate(Vector3.forward * rotationSpeed * Time.deltaTime);
                transform.position += (Vector3)(moveDirection * moveSpeed * Time.deltaTime);

                if (transform.position.x < minBounds.x - boundsOffset || transform.position.x > maxBounds.x + boundsOffset ||
                    transform.position.y < minBounds.y - boundsOffset || transform.position.y > maxBounds.y + boundsOffset)
                {
                    DestroyStarServerRpc(0);
                }
            }
        }
    }

    public void OnClick()
    {
        if (IsSpawned)
        {
            Debug.Log("클릭");
            GameObject effect = Instantiate(shotEffect, transform.position, transform.rotation);
            Destroy(effect, 2f);
            DestroyStarServerRpc(0f);
        }
    }

    private IEnumerator ResetCoroutine(float duration)
    {
        NetworkObject networkObject = GetComponent<NetworkObject>();

        float timer = 0f;

        while (timer < duration)
        {
            if (networkObject == null || !networkObject.IsSpawned) 
                yield break;

            timer += Time.deltaTime;

            yield return null;
        }

        if (networkObject != null && networkObject.IsSpawned)
        {
            networkObject.Despawn();
        }

        destroyCoroutine = null;
        Destroy(gameObject);
    }

    private void InitPosition()
    {
        Vector3 randomPos = new Vector3(Random.Range(-5f, 5f), Random.Range(-3f, 3f), 0);
        transform.position = randomPos;
        rotationSpeed = Random.Range(-100f, 100f);
        float angle = Random.Range(0f, 360f);
        moveDirection = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
        moveSpeed = Random.Range(1f, 3f);
    }

    [ServerRpc(RequireOwnership = false)]
    private void DestroyStarServerRpc(float duration)
    {
        if (!IsSpawned) return;
        if (destroyCoroutine == null)
        {
            Debug.Log($"[Netcode] DestroyStarServerRpc 실행: {gameObject.name} (NetworkObjectId: {NetworkObjectId})");
            destroyCoroutine = StartCoroutine(ResetCoroutine(duration));
        }
    }
}