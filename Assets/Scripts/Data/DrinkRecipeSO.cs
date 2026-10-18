using System.Collections.Generic;
using UnityEngine;

/// <summary>A recipe/menu item: what a correct drink contains.</summary>
[CreateAssetMenu(menuName = "Coffee/Drink Recipe")]
public class DrinkRecipeSO : ScriptableObject
{
    public string id;
    public string displayName;
    public int basePriceCents = 450;
    public IngredientSO baseIngredient;
    public IngredientSO milk;                         // may be null (black coffee)
    public List<IngredientSO> syrups = new();
    public List<IngredientSO> toppings = new();
    public float idealBrewSeconds = 25f;              // optional timing target
    public int unlockDay = 1;
}
