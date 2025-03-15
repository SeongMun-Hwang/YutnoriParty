using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
public class RunGameController : NetworkBehaviour
{
    [SerializeField] private float moveDistance = 2f;  // 한 번 이동할 거리
    [SerializeField] private float moveSpeed = 5f;     // 이동 속도

    private Vector3 targetPosition;
    private Animator animator;
    public NetworkVariable<bool> canMove = new NetworkVariable<bool>(false);

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            Transform spawnTransform = RunGameManager.Instance.spawnPos[PlayerManager.Instance.GetClientIndex(OwnerClientId)];
            targetPosition = spawnTransform.position;
            transform.position = targetPosition;
        }
    }

    private void Update()
    {
        if (!IsOwner) return;
        {
            if (RunGameManager.Instance != null)
            {
                if (RunGameManager.Instance.runGameCamera != null)
                {
                    Camera cam = RunGameManager.Instance.runGameCamera;
                    cam.transform.position = transform.position + new Vector3(0, 4, 7);
                    cam.transform.rotation = Quaternion.Euler(6f, -180f, 0f);
                }
                else
                {
                    
                }
            }
            else
            {
                
            }
            if (!canMove.Value) return;
            {
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    MoveForwardServerRpc((ulong)PlayerManager.Instance.GetClientIndex(OwnerClientId));
                    AudioManager.instance.Playsfx(4);
                }

                transform.position = Vector3.Lerp(transform.position, targetPosition, moveSpeed * Time.deltaTime);
                float moveSpeedValue = Vector3.Distance(transform.position, targetPosition) > 0.01f ? 1f : 0f;
                GetComponent<Animator>().SetFloat("MoveSpeed", moveSpeedValue);
            }
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
