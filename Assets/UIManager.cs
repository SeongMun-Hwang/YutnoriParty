using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
	public Button HostButton;
	public Button ClientButton;
	public TMP_InputField InputField;

	private void Start()
	{
		HostButton.onClick.AddListener(() => HostButtonClicked());
		ClientButton.onClick.AddListener(() => ClientButtonClicked());
	}

	public void HostButtonClicked()
	{
		NetworkManager.Singleton.StartHost();
	}

	public void ClientButtonClicked()
	{
		NetworkManager.Singleton.StartClient();
	}
}
