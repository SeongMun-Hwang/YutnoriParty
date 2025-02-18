using System.Collections.Generic;
using UnityEngine;

public class Node : MonoBehaviour
{
    public List<Node> nextNode;
    public List<Node> prevNode;

    public Node GetNextNode()
    {
        if (nextNode.Count==1)
        {
            return nextNode[0];
        }
        if (nextNode.Count > 1)
        {
            return nextNode[Random.Range(0, nextNode.Count)];
        }
        return null;
    }
    public Node GetPrevNode()
    {
        if (prevNode.Count == 0)
        {
            return prevNode[0];
        }
        if (prevNode.Count > 0)
        {
            return prevNode[Random.Range(0, prevNode.Count)];
        }
        return null;
    }
}
