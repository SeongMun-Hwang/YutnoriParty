using NUnit.Framework;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CharacterInfo : NetworkBehaviour
{
    public NetworkVariable<bool> inIsland = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone);
    public NetworkVariable<bool> canMove = new NetworkVariable<bool>(true, NetworkVariableReadPermission.Everyone);
    public int overlappedCount = 0;
    public NetworkVariable<bool> isReverse = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone);
    public GameObject ItemEffect;
    public List<CharacterBoardMovement> childs = new List<CharacterBoardMovement>();
    [SerializeField] GameObject pyramid;
    [SerializeField] Material characterColor;
    private string userName;

    public override void OnNetworkSpawn()
    {
        pyramid.GetComponent<Renderer>().material = characterColor;
        userName = PlayerManager.Instance.RetrunPlayerName(GetComponent<NetworkObject>().OwnerClientId);
        pyramid.GetComponent<PlayerPyramid>().SetUserName(userName);
    }
    [ServerRpc(RequireOwnership = false)]
    public void SetPyramidServerRpc(bool isActive)
    {
        SetPyramidClientRpc(isActive, overlappedCount);
    }
    [ClientRpc]
    public void SetPyramidClientRpc(bool isActive, int overlapped = 0)
    {
        pyramid.GetComponent<PlayerPyramid>().overlappedHeight = overlapped * 2f;
        if (isActive)
        {
            pyramid.SetActive(true);
        }
        else
        {
            pyramid.SetActive(false);
        }
    }
}
