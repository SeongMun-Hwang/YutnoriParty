using UnityEngine;

public class LookAtCamera : MonoBehaviour
{
    private void Update()
    {
        if(Camera.main != null)
        {
            transform.forward = -Camera.main.transform.forward;
        }
    }
}
