using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;

public class ShootableStar : MonoBehaviour
{
    private float rotationSpeed; // 회전 속도
    private Vector2 moveDirection; // 이동 방향
    private float moveSpeed; // 이동 속도
    private Vector2 minBounds, maxBounds; // 화면 바운더리

    [SerializeField] private GameObject shotEffect;
    private Animator _animator;

    private void Start()
    {
        _animator = GetComponent<Animator>();
        ResetPosition();

        Camera cam = Camera.main;
        minBounds = cam.ViewportToWorldPoint(new Vector3(0, 0, cam.nearClipPlane));
        maxBounds = cam.ViewportToWorldPoint(new Vector3(1, 1, cam.nearClipPlane));
    }

    private void Update()
    {
        transform.Rotate(Vector3.forward * rotationSpeed * Time.deltaTime);
        transform.position += (Vector3)(moveDirection * moveSpeed * Time.deltaTime);

        if (transform.position.x < minBounds.x || transform.position.x > maxBounds.x ||
            transform.position.y < minBounds.y || transform.position.y > maxBounds.y)
        {
            Destroy(gameObject); // 별 삭제
        }
    }

    public void OnClick()
    {
        Debug.Log("클릭");
        GameObject effect = Instantiate(shotEffect, transform.position, transform.rotation);
        Destroy(effect, 2f);
        StartCoroutine(ResetCoroutine());
    }

    private IEnumerator ResetCoroutine()
    {
        _animator.SetTrigger("Destroy");
        yield return new WaitForSeconds(1.5f);
        Destroy(gameObject);
    }

    private void ResetPosition()
    {
        Vector3 randomPos = new Vector3(Random.Range(-5f, 5f), Random.Range(-3f, 3f), 0);
        transform.position = randomPos;
        rotationSpeed = Random.Range(-100f, 100f);
        float angle = Random.Range(0f, 360f);
        moveDirection = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
        moveSpeed = Random.Range(1f, 3f);
    }
}