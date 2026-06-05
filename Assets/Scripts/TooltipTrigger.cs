using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
    public string Text;

    private static readonly System.Collections.Generic.List<TooltipTrigger> ActiveTriggers = new System.Collections.Generic.List<TooltipTrigger>();
    private GameObject tooltip;
    private RectTransform tooltipRect;

    public static void HideAll()
    {
        for (int i = ActiveTriggers.Count - 1; i >= 0; i--)
        {
            if (ActiveTriggers[i] != null)
                ActiveTriggers[i].Hide();
        }

        ActiveTriggers.Clear();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (string.IsNullOrWhiteSpace(Text) || tooltip != null)
            return;

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
            return;

        Image panel = RuntimeUI.Panel(canvas.transform, "Tooltip", new Color(0.06f, 0.075f, 0.10f, 0.96f), Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
        panel.raycastTarget = false;
        tooltip = panel.gameObject;
        tooltipRect = tooltip.GetComponent<RectTransform>();
        tooltipRect.anchorMin = Vector2.zero;
        tooltipRect.anchorMax = Vector2.zero;
        tooltipRect.pivot = new Vector2(0f, 1f);
        tooltipRect.sizeDelta = new Vector2(420f, 92f);

        TMP_Text label = RuntimeUI.Text(tooltip.transform, "Tooltip Text", Text, 22, RuntimeUI.Paper, TextAlignmentOptions.MidlineLeft);
        label.raycastTarget = false;
        RuntimeUI.SetRect(label, Vector2.zero, Vector2.one, new Vector2(18f, 12f), new Vector2(-18f, -12f));
        if (!ActiveTriggers.Contains(this))
            ActiveTriggers.Add(this);
        Move(eventData);
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        Move(eventData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Hide();
    }

    private void OnDisable()
    {
        Hide();
    }

    private void OnDestroy()
    {
        Hide();
    }

    private void Hide()
    {
        if (tooltip != null)
            Destroy(tooltip);
        tooltip = null;
        tooltipRect = null;
        ActiveTriggers.Remove(this);
    }

    private void Move(PointerEventData eventData)
    {
        if (tooltipRect == null)
            return;

        tooltipRect.position = eventData.position + new Vector2(18f, -18f);
    }
}
