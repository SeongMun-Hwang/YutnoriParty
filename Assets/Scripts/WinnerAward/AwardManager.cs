using TMPro;
using UnityEngine;

public class AwardManager : MonoBehaviour
{
    [SerializeField] TMP_Text winnerName;

    void Start()
    {
        AudioManager.instance.Playsfx(12);
        // winnerName = "";
    }
}
