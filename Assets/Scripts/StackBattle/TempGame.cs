using Unity.Netcode;
using UnityEngine;

public class TempGame : NetworkBehaviour
{
	[SerializeField] private GameObject StackBattleGame;
	[SerializeField] private GameObject MinigameView;

	public void StackBattleButtonPress()
	{
		if (NetworkManager.Singleton.IsClient)
		{
			RequestStartStackBattleServerRpc();
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void RequestStartStackBattleServerRpc()
	{
		StartGameClientRpc();
	}

	[ClientRpc]
	private void StartGameClientRpc()
	{
		Debug.Log("�̴ϰ��� ����!");
		StackBattleGame.SetActive(true);
		MinigameView.SetActive(true);
	}
}
