using System;
using UnityEngine;

public class YutOutTrigger : MonoBehaviour
{
    int triggerExit = 0;
    int triggered = 0;
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("뭔가 들어옴");

        triggerExit--;
        if(triggerExit == 0)
        {
            YutManager.Instance.YutFalledRpc(false);
        }

        triggered++;
    }

    private void OnTriggerExit(Collider other)
    {
        Debug.Log("뭔가 나감");

        triggerExit++;
        if (triggerExit > 0)
        {
            Debug.Log("낙 감지");
            YutManager.Instance.YutFalledRpc(true);
        }

        triggered--;
    }
}