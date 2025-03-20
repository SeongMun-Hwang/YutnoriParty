using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConfirmMessageController : MonoBehaviour
{
	[SerializeField]
	private TMP_Text Message;

	public Button Accept;
	public Button Deny;

	public Action OnAccept; // 확인할 때 실행할 동작

	private bool isInitialized = false;

	private void OnEnable()
	{
		AudioManager.instance.Playsfx(13);
	}

	public void OnAcceptPressed()
	{
		if (!isInitialized) return;
        AudioManager.instance.Playsfx(13);
        OnAccept.Invoke();
		Close();
	}

	public void Init(string msg, Action onAccept)
	{
		Message.text = msg;
		OnAccept = onAccept;
		isInitialized = true;
	}

	public void Close()
	{
        AudioManager.instance.Playsfx(13);
		Destroy(gameObject);
	}
}
