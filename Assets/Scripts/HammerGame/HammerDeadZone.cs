using Unity.Netcode;
using UnityEngine;

public class HammerDeadZone : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Character"))
        {
            Debug.Log("zone : " + gameObject.GetComponent<NetworkObject>() == null);
            HammerGameManager.Instance.DespawnLoserServerRpc(collision.gameObject.GetComponent<NetworkObject>());
        }
    }
}
