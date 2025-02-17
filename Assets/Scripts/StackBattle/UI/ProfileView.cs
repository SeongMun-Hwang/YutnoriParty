using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ProfileView : MonoBehaviour
{
	[SerializeField] private Image profileImage;
	[SerializeField] private TMP_Text nameText;
	[SerializeField] private TMP_Text scoreText;

	// 뷰 초기화
	public void InitView(PlayerProfile pp)
	{
		nameText.text = pp.Name;
		scoreText.text = pp.Score.ToString();
	}

	// 점수 변경
	public void ChangeScore(PlayerProfile pp)
	{
		scoreText.text = pp.Score.ToString();
	}
}
