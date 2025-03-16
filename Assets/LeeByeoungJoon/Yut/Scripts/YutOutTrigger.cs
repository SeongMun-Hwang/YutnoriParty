using System;
using System.Collections.Generic;
using UnityEngine;

public class YutOutTrigger : MonoBehaviour
{
    List<Rigidbody> rbs = new List<Rigidbody>();

    public Action<int> OnYutTriggerd;
    int yutTriggered = 0;

    [SerializeField] Transform tmpPos;

    //윷 들어와 있는 개수 확인
    private void OnTriggerEnter(Collider other)
    {
        if(other.tag == "Yut")
        {
            yutTriggered++;
            
            rbs.Add(other.GetComponent<Rigidbody>());
            HoldYuts();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Yut")
        {
            yutTriggered--;
        }
    }

    //너무 멀리 떨어져서 버그나는거 방지하려고 화면 밖에 잡아둠
    public void HoldYuts()
    {
        foreach(var rb in rbs)
        {
            rb.isKinematic = true;
            rb.gameObject.transform.position = tmpPos.position;
        }
    }

    //잡아둔 윷 키네마틱 해제
    public void ReleaseYuts()
    {
        foreach (var rb in rbs)
        {
            rb.isKinematic = false;
        }
        rbs.Clear();
    }
}
