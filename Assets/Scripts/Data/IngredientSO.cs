using System.Collections.Generic;
using UnityEngine;

/// <summary>One ingredient the player can add. Content only, no logic.</summary>
[CreateAssetMenu(menuName = "Coffee/Ingredient")]
public class IngredientSO : ScriptableObject
{
    public string id;                 // stable key, e.g. "espresso", "oat_milk"
    public string displayName;
    public IngredientKind kind;
    public Sprite icon;               // 2D UI icon (from the 2D supplies pack or generated)
    public GameObject prefab3D;       // optional wrapper prefab for the physical prop
    public int cost;                  // cents, cost to you
    public int unlockDay = 1;
}
