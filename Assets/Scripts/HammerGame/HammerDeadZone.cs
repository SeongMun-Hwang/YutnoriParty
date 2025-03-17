using Unity.Netcode;
using UnityEngine;

public class HammerDeadZone : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Character"))
        {
            Debug.Log("player falled");
            HammerGameManager.Instance.DespawnLoserServerRpc(collision.gameObject.GetComponent<NetworkObject>());
        }
    }
}
