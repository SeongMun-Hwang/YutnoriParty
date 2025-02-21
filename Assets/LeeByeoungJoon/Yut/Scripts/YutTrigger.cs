using System;
using UnityEngine;

public class YutTrigger : MonoBehaviour
{
    public Action<int> OnYutTriggerd;
    int yutTriggered = 0;

    //윷 들어와 있는 개수 확인
    private void OnTriggerEnter(Collider other)
    {
        if(other.tag == "Yut")
        {
            yutTriggered++;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Yut")
        {
            yutTriggered--;
        }
    }
}
