using UnityEngine;
using UnityEngine.EventSystems;

public class DraggablePanel : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    [SerializeField] private bool clampToCanvas = true;

    private RectTransform rectTransform;
    private Canvas canvas;
    private Vector2 pointerStart;
    private Vector2 panelStart;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        pointerStart = eventData.position;
        panelStart = rectTransform != null ? rectTransform.anchoredPosition : Vector2.zero;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (rectTransform == null)
            return;

        float scale = canvas != null ? Mathf.Max(0.01f, canvas.scaleFactor) : 1f;
        Vector2 target = panelStart + (eventData.position - pointerStart) / scale;
        rectTransform.anchoredPosition = clampToCanvas ? Clamp(target) : target;
    }

    private Vector2 Clamp(Vector2 value)
    {
        if (canvas == null)
            return value;

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        if (canvasRect == null)
            return value;

        Rect canvasBounds = canvasRect.rect;
        Rect panelBounds = rectTransform.rect;
        float maxX = Mathf.Max(0f, (canvasBounds.width - panelBounds.width) * 0.5f);
        float maxY = Mathf.Max(0f, (canvasBounds.height - panelBounds.height) * 0.5f);
        return new Vector2(Mathf.Clamp(value.x, -maxX, maxX), Mathf.Clamp(value.y, -maxY, maxY));
    }
}
