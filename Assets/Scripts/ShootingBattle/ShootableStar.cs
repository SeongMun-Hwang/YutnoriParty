using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;

public class ShootableStar : MonoBehaviour
{
    private float rotationSpeed; // 회전 속도
    private Vector2 moveDirection; // 이동 방향
    private float moveSpeed; // 이동 속도

    [SerializeField] private GameObject shotEffect;

    private void Start()
    {
        rotationSpeed = Random.Range(-100f, 100f);
        float angle = Random.Range(0f, 360f);
        moveDirection = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
        moveSpeed = Random.Range(1f, 4f);
    }

    private void Update()
    {
        transform.Rotate(Vector3.forward * rotationSpeed * Time.deltaTime);
        transform.position += (Vector3)(moveDirection * moveSpeed * Time.deltaTime);
    }

    public void OnClick()
    {
        Debug.Log("클릭");
        GameObject effect = Instantiate(shotEffect, transform.position, transform.rotation);
        Destroy(effect, 2f);
    }
}