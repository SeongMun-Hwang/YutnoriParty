using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ApplicationManager : MonoBehaviour
{
    async void Start()
    {
        DontDestroyOnLoad(gameObject);
        //�������� ��������Ʈ ������ ������ �� �׷��� ���� �ַܼ� ����
        await LaunchMode(SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null);
    }

    async Task LaunchMode(bool isDedicateServer)
    {
            bool authenticated = await ClientSingleton.Instance.InitAsync();
            HostSingleton hostSingleton = HostSingleton.Instance;
            if (authenticated)
            {
                GotoMenu();
            }        
    }
    public void GotoMenu()
    {
        SceneManager.LoadScene("MenuScene");
    }
}