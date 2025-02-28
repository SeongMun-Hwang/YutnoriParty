using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EventNodeData", menuName = "Scriptable Objects/EventNodeData")]
public class EventNodeData : ScriptableObject
{
    public EventNodeType eventNodeType;
    public int minNode;
    public int maxNode;
    public int lifeTime;

    public List<Vector3> spawnPositions;
}
