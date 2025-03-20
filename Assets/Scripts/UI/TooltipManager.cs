using TMPro;
using UnityEngine;

public class TooltipManager : MonoBehaviour
{
    private static TooltipManager instance;
    public static TooltipManager Instance { get { return instance; } }

    public enum TooltipType
    {
        None,
        Item,
        SpecialNode,
    }

    [SerializeField] private GameObject tooltipCanvas;
    [SerializeField] private RectTransform tooltipPanel;
    [SerializeField] private TMP_Text tooltipTitle;
    [SerializeField] private TMP_Text tooltipType;
    [SerializeField] private TMP_Text tooltipDescription;

    private Transform lastHitObject = null;

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

    private void RaycastObjectAndDrawTooltip()
    {
        if (tooltipCanvas == null || Camera.main == null) { return; }
        if (MinigameManager.Instance.minigameStart.Value) 
        {
            EraseTooltip();
            return; 
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, LayerMask.GetMask("SpecialNode")))
        {
            //Debug.Log(hit.collider.gameObject.name);
            if (hit.collider.gameObject.name.Contains("Trigger"))
            {
                Transform currentHitObject = hit.collider.transform.parent;

                if (lastHitObject != currentHitObject)
                {
                    lastHitObject = currentHitObject;
                    if (currentHitObject.name.Contains("BlackHoleNode"))
                    {
                        DrawTooltip("블랙홀", "모든 직선 범위에 존재하는 말들을 현재 위치로 끌고 옵니다.", TooltipType.SpecialNode);
                    }
                    else if (currentHitObject.name.Contains("IslandNode"))
                    {
                        DrawTooltip("무인도", "한 턴 동안 이 노드에 갇히게 됩니다.", TooltipType.SpecialNode);
                    }
                    else if (currentHitObject.name.Contains("ItemNode"))
                    {
                        DrawTooltip("아이템 박스", "랜덤한 아이템을 획득합니다.", TooltipType.SpecialNode);
                    }
                }
            }
            else if (hit.collider.gameObject.name.Contains("Obstacle"))
            {
                Transform currentHitObject = hit.collider.transform;
                if (lastHitObject != currentHitObject)
                {
                    lastHitObject = currentHitObject;
                    ulong id = hit.collider.GetComponent<Obstacle>().ownerId.Value;
                    DrawTooltip("장애물", $"장애물에 부딪힌 말은 이 자리에서 이동을 멈춥니다." +
                        $"<br>설치 <color=#99EFFF>{PlayerManager.Instance.RetrunPlayerName(id)}</color>", TooltipType.SpecialNode
                        );
                }
            }
        }
        else
        {
            if (lastHitObject != null)
            {
                lastHitObject = null;
                EraseTooltip();
            }
        }
    }

    private void UpdatePosition()
    {
        if (tooltipCanvas == null) { return; }
        float width = tooltipPanel.rect.width;
        float height = tooltipPanel.rect.height;

        Vector2 mousePos = Input.mousePosition;

        Vector2 offset = new Vector2(width / 2, -height / 2);

        float tooltipX = Mathf.Clamp(mousePos.x + offset.x, width / 2, Screen.width - width / 2);
        float tooltipY = Mathf.Clamp(mousePos.y + offset.y, height / 2, Screen.height - height / 2);

        tooltipPanel.transform.position = new Vector2(tooltipX, tooltipY);
    }

    private void Update()
    {
        if (Input.GetKey(KeyCode.F1))
        {
            ItemManager.Instance.GetItemClientRpc(0);
        }

        UpdatePosition();
        RaycastObjectAndDrawTooltip();
    }

    public void DrawTooltip(string title, string description, TooltipType type = TooltipType.None)
    {
        if (tooltipCanvas == null) { return; }

        tooltipTitle.text = title;
        tooltipDescription.text = description;

        switch (type)
        {
            case TooltipType.None:
                tooltipType.text = "";
                break;
            case TooltipType.Item:
                tooltipType.text = "<color=#B0CF53>아이템</color>";
                break;
            case TooltipType.SpecialNode:
                tooltipType.text = "<color=#D899FF>특수 칸</color>";
                break;
        }

        tooltipCanvas.SetActive(true);
    }

    public void EraseTooltip()
    {
        if (tooltipCanvas == null) { return; }
        tooltipCanvas.gameObject.SetActive(false);
    }

    public bool IsTooltipActive()
    {
        if (tooltipCanvas == null) { return false; }
        return tooltipCanvas.activeSelf;
    }
}
