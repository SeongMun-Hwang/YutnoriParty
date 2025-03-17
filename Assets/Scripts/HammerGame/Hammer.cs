using Unity.Netcode;
using UnityEngine;

public class Hammer : NetworkBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Character") 
            && collision.gameObject!=transform.root.gameObject)
        {
            NetworkObjectReference noRef = collision.gameObject.GetComponent<NetworkObject>();
            Vector3 forceDir = transform.root.transform.forward+transform.root.transform.up*0.025f;
            HammerGameManager.Instance.AddForceWithHammerServerRpc(noRef,forceDir);
        }
    }
}
