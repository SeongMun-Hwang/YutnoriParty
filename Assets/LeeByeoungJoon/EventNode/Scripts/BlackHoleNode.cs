using UnityEngine;

public class BlackHoleNode : EventNode
{
   

    public override void EventStart()
    {
        base.EventStart();

        Debug.Log("블랙홀 밟음");
    }
}
