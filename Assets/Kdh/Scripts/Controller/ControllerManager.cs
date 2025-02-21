using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class ControllerManager : NetworkBehaviour
{
    private Dictionary<string, MonoBehaviour> controllers = new Dictionary<string, MonoBehaviour>();

    private void Awake()
    {
        // 플레이어 오브젝트에 있는 모든 컨트롤러를 찾아서 저장
        MonoBehaviour[] allControllers = GetComponents<MonoBehaviour>();

        foreach (MonoBehaviour controller in allControllers)
        {
            if (controller is NetworkBehaviour && controller != this)
            {
                controllers[controller.GetType().Name] = controller;
                controller.enabled = false; // 처음엔 다 꺼놓음
            }
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsOwner) return;

        ApplyControllerBasedOnScene();
    }

    private void ApplyControllerBasedOnScene()
    {
        string currentScene = SceneManager.GetActiveScene().name;

        // 모든 컨트롤러 비활성화
        foreach (var controller in controllers.Values)
        {
            controller.enabled = false;
        }

        // 현재 씬 이름과 같은 컨트롤러가 있으면 활성화
        if (controllers.ContainsKey(currentScene + "Controller"))
        {
            controllers[currentScene + "Controller"].enabled = true;
        }
    }
}
