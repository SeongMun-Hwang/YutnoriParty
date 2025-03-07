using UnityEngine;

public class CrowdController : MonoBehaviour
{
    void Start()
    {
        int type = Random.Range(0, 9);
        GetComponent<Animator>().SetInteger("type", type);
    }
}
