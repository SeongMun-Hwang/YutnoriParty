using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
public class RunGameController : NetworkBehaviour
{
    [SerializeField] private float moveDistance = 2f;  // 한 번 이동할 거리
    [SerializeField] private float moveSpeed = 5f;     // 이동 속도

    private Vector3 targetPosition;
    private Animator animator;
    public NetworkVariable<bool> canMove = new NetworkVariable<bool>(true);

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            Transform spawnTransform = RunGameManager.Instance.spawnPos[(int)OwnerClientId];
            targetPosition = spawnTransform.position;
        }
    }

    private void Update()
    {
        if (IsOwner && canMove.Value)
        {
            if (Camera.main != null)
            {
                Camera.main.transform.position = transform.position + new Vector3(0, 4, 7);
                Camera.main.transform.rotation = Quaternion.Euler(6f, -180f, 0f);
            }
            if (Input.GetKeyDown(KeyCode.Space))
            {
                MoveForwardServerRpc(OwnerClientId);
            }

            transform.position = Vector3.Lerp(transform.position, targetPosition, moveSpeed * Time.deltaTime);
            float moveSpeedValue = Vector3.Distance(transform.position, targetPosition) > 0.01f ? 1f : 0f;
            GetComponent<Animator>().SetFloat("MoveSpeed", moveSpeedValue);
        }
    }

    [ServerRpc]
    private void MoveForwardServerRpc(ulong clientId)
    {

        if (OwnerClientId != clientId) return;

        targetPosition += transform.forward * moveDistance;
        MoveClientRpc(targetPosition);
    }

    [ClientRpc]
    private void MoveClientRpc(Vector3 newPosition)
    {
        targetPosition = newPosition;
    }
    public void EnableControl(bool enable)
    {
        canMove.Value = enable;
    }


}
