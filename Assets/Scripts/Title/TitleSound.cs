using UnityEngine;

public class TitleSound : MonoBehaviour
{
    public void UIButtonClicked()
    {
        AudioManager.instance.Playsfx(13);
    }
}
