using System.Collections.Generic;
using UnityEngine;

public class Node : MonoBehaviour
{
    [SerializeField] List<Node> nextNode;
    [SerializeField] List<Node> prevNode;

    public List<Node> GetNextNode()
    {
        return nextNode;
    }
    public List<Node> GetPrevNode()
    {
        return prevNode;
    }
}
