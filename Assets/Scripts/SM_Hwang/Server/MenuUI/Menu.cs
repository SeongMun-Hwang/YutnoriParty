using TMPro;
using Unity.Netcode;
using Unity.Services.Authentication;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Menu : MonoBehaviour
{
    [SerializeField] TMP_InputField joinCodeField;
    [SerializeField] TMP_InputField userNameField;
    [SerializeField] TMP_InputField roomNameField;
    [SerializeField] GameObject confirmMessageCanvas;

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
        if (roomNameField.text.Length > 12)
        {
            ConfirmMessageController confirm =
                Instantiate(confirmMessageCanvas).GetComponent<ConfirmMessageController>();
            confirm.Init($"방 제목은 12글자를 넘을 수 없습니다", () =>
            {

            });
        }
        else
        {
            await HostSingleton.Instance.StartHostAsync(roomNameField.text);
        }
    }

    public async void StartClient()
    {
        await ClientSingleton.Instance.StartClientAsync(joinCodeField.text);
    }

    public async void ChangeName()
    {
        if (userNameField.text.Length > 6)
        {
            ConfirmMessageController confirm =
                Instantiate(confirmMessageCanvas).GetComponent<ConfirmMessageController>();
            confirm.Init($"닉네임은 6글자를 넘을 수 없습니다", () =>
            {

            });
        }
        else
        {
            await AuthenticationService.Instance.UpdatePlayerNameAsync(userNameField.text);
        }
        
    }

    public void ExiteGame()
    {
        ConfirmMessageController confirm =
                Instantiate(confirmMessageCanvas).GetComponent<ConfirmMessageController>();
        confirm.Init($"게임을 종료합니다", () =>
        {
#if UNITY_EDITOR
            EditorApplication.ExitPlaymode();
#else
        Application.Quit();
#endif
        });
    }
}