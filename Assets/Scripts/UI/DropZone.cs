// Scripts/UI/DropZone.cs
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Put this on a UI element with a Graphic (e.g. an Image, raycast target ON).
/// Reports any DraggableIngredient dropped on it. Reusable for cup, milk
/// pitcher, pastry plate, etc.
/// </summary>
[RequireComponent(typeof(Graphic))]
public class DropZone : MonoBehaviour, IDropHandler
{
    public event Action<IngredientSO> IngredientDropped;

    [Tooltip("Optional filter: only accept these kinds. Leave empty to accept all.")]
    public IngredientKind[] acceptedKinds;

    public void OnDrop(PointerEventData e)
    {
        if (e.pointerDrag == null) return;
        var drag = e.pointerDrag.GetComponent<DraggableIngredient>();
        if (drag == null || drag.ingredient == null) return;
        if (!Accepts(drag.ingredient)) return;

        IngredientDropped?.Invoke(drag.ingredient);
    }

    bool Accepts(IngredientSO ing)
    {
        if (acceptedKinds == null || acceptedKinds.Length == 0) return true;
        foreach (var k in acceptedKinds) if (k == ing.kind) return true;
        return false;
    }
}
