using NUnit.Framework;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class InGameCanvas : NetworkBehaviour
{
    List<int> yutResult = new List<int>();
    public void ThrowYut()
    {
        if ((ulong)GameManager.Instance.mainGameProgress.currentPlayerNumber == NetworkManager.LocalClientId)
        {
            yutResult.Add(Random.Range(1, 5));
        }
    }
}