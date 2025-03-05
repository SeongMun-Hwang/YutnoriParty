using System;
using UnityEngine;

public class YutOut : MonoBehaviour
{
    public Action<int> OnYutCollided;
    int yutCollided = 0;

    //윷 들어와 있는 개수 확인
    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.tag == "Yut")
        {
            yutCollided++;
            OnYutCollided?.Invoke(yutCollided);
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.tag == "Yut")
        {
            yutCollided--;
            OnYutCollided?.Invoke(yutCollided);
        }
    }
}
