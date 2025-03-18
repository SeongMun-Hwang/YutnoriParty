using System.Collections;
using UnityEngine;

public class CrowdController : MonoBehaviour
{
    public StackBattleManager manager;
    public float jumpHeight = 1.0f;  // 점프 높이
    public float jumpDuration = 0.2f;  // 점프하는 시간

    private void Start()
    {
        int type = Random.Range(0, 9);
        GetComponent<Animator>().SetInteger("type", type);
        manager.currentTurnPlayerId.OnValueChanged += Jump;
    }

    public void Jump(ulong oldId, ulong newId)
    {
        StartCoroutine(JumpCoroutine());
    }

    private IEnumerator JumpCoroutine()
    {
        yield return new WaitForSeconds(Random.Range(0, 0.5f));
        float elapsedTime = 0f;

        Vector3 startPos = transform.position;

        // 점프 상승
        while (elapsedTime < jumpDuration)
        {
            float deltaY = (jumpHeight / jumpDuration) * Time.deltaTime;
            transform.Translate(0, deltaY, 0, Space.World);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        elapsedTime = 0f;

        // 점프 하강
        while (elapsedTime < jumpDuration / 2)
        {
            float deltaY = (jumpHeight / jumpDuration) * Time.deltaTime;
            transform.Translate(0, -deltaY, 0, Space.World);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // 위치 보정 (숫자 오차로 인해 정확한 원위치 보장)
        transform.position = startPos;

        int type = Random.Range(0, 9);
        GetComponent<Animator>().SetInteger("type", type);
    }
}
