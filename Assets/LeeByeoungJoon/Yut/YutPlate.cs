using System;
using UnityEngine;

public class YutPlate : MonoBehaviour
{
    public Action<int> OnYutGrounded;
    int yutGrounded = 0;

    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.tag == "Yut")
        {
            yutGrounded++;
            OnYutGrounded?.Invoke(yutGrounded);
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.tag == "Yut")
        {
            yutGrounded--;
            OnYutGrounded?.Invoke(yutGrounded);
        }
    }
}
