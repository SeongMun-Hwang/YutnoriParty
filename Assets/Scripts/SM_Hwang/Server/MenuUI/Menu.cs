using TMPro;
using Unity.Netcode;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Menu : MonoBehaviour
{
    [SerializeField] TMP_InputField joinCodeField;
    [SerializeField] TMP_InputField userNameField;
    [SerializeField] TMP_InputField roomNameField;
    private void Start()
    {
        if (GameObject.FindFirstObjectByType<NetworkManager>() == null)
        {
            SceneManager.LoadScene("NetConnect");
        }
        try
        {
            string username = AuthenticationService.Instance.PlayerName ?? "";
            if (username.Contains("#"))
            {
                username = username.Substring(0, username.IndexOf("#"));
            }
            userNameField.text = username;
        }
        catch
        {

        }
    }
    public async void StartHost()
    {
        await HostSingleton.Instance.StartHostAsync(roomNameField.text);
    }
    public async void StartClient()
    {
        await ClientSingleton.Instance.StartClientAsync(joinCodeField.text);
    }
    public async void ChangeName()
    {
        await AuthenticationService.Instance.UpdatePlayerNameAsync(userNameField.text);
    }
}