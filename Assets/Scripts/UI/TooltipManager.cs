using TMPro;
using UnityEngine;

public class TooltipManager : MonoBehaviour
{
    private static TooltipManager instance;
    public static TooltipManager Instance { get { return instance; } }

    [SerializeField] private GameObject tooltipCanvas;
    [SerializeField] private RectTransform tooltipPanel;
    [SerializeField] private TMP_Text tooltipTitle;
    [SerializeField] private TMP_Text tooltipDescription;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    public void DrawTooltip(string title, string description)
    {
        tooltipTitle.text = title;
        tooltipDescription.text = description;
        tooltipCanvas.SetActive(true);
    }

    public void EraseTooltip()
    {
        tooltipCanvas.gameObject.SetActive(false);
    }

    public bool IsTooltipActive()
    {
        return tooltipCanvas.activeSelf;
    }
}
