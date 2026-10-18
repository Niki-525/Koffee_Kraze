// Scripts/UI/DraggableIngredient.cs
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// A tray item. Dragging it spawns a "ghost" icon that follows the pointer;
/// the tray item itself never moves, so the supply is effectively infinite.
/// Drop it on a DropZone to add the ingredient to the drink.
/// </summary>
[RequireComponent(typeof(Image))]
public class DraggableIngredient : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public IngredientSO ingredient;
    [Tooltip("Optional name label shown on the tray item.")]
    public TMP_Text label;
    [Tooltip("Optional child Image that shows the icon. If empty, the icon is drawn on this object's own Image.")]
    public Image iconImage;

    Canvas _canvas;
    GameObject _ghost;
    RectTransform _ghostRect;

    public void Setup(IngredientSO ing)
    {
        ingredient = ing;
        var img = iconImage != null ? iconImage : GetComponent<Image>();
        img.sprite = ing.icon;
        img.preserveAspect = true;
        if (iconImage != null) iconImage.enabled = ing.icon != null;   // no sprite = no white box
        if (ing.icon == null)
            Debug.LogWarning($"Ingredient \"{ing.id}\" has no icon sprite assigned.", ing);
        if (label) label.text = ing.displayName;
        name = $"Tray_{ing.id}";
    }

    public void OnBeginDrag(PointerEventData e)
    {
        if (ingredient == null) return;

        _canvas = GetComponentInParent<Canvas>().rootCanvas;

        _ghost = new GameObject("DragGhost", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        _ghost.transform.SetParent(_canvas.transform, false);
        _ghost.transform.SetAsLastSibling();

        var img = _ghost.GetComponent<Image>();
        img.sprite = ingredient.icon;
        img.preserveAspect = true;
        img.raycastTarget = false;
        _ghost.GetComponent<CanvasGroup>().blocksRaycasts = false;

        _ghostRect = _ghost.GetComponent<RectTransform>();
        _ghostRect.sizeDelta = ((RectTransform)transform).rect.size;
        MoveGhost(e);
    }

    public void OnDrag(PointerEventData e) => MoveGhost(e);

    public void OnEndDrag(PointerEventData e)
    {
        if (_ghost != null) Destroy(_ghost);
        _ghost = null;
    }

    void MoveGhost(PointerEventData e)
    {
        if (_ghostRect == null) return;
        Camera cam = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
        RectTransformUtility.ScreenPointToWorldPointInRectangle(
            (RectTransform)_canvas.transform, e.position, cam, out Vector3 world);
        _ghostRect.position = world;
    }

    void OnDisable()
    {
        if (_ghost != null) Destroy(_ghost);
    }
}
