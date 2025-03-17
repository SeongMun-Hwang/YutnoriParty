using Unity.Netcode;
using UnityEngine;

public class Hammer : NetworkBehaviour
{
    private float hammerPower = 300f;
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Character") 
            && collision.gameObject!=transform.root.gameObject)
        {
            collision.gameObject.GetComponent<HammerGameController>().PlayHitSound();
            NetworkObjectReference noRef = collision.gameObject.GetComponent<NetworkObject>();
            Vector3 forceDir = (transform.root.transform.forward+transform.root.transform.up) * hammerPower;
            HammerGameManager.Instance.AddForceWithHammerServerRpc(noRef,forceDir);
            
            //업그레이드
            gameObject.transform.localScale*= 1.1f;
            hammerPower += 50f;
            Debug.Log(OwnerClientId+"'s hammerPower : " + hammerPower);  
        }
    }
}
