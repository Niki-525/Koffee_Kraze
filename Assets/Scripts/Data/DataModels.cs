// Scripts/Data/DataModels.cs
using System;
using System.Collections.Generic;
using UnityEngine;

public enum IngredientKind { Base, Milk, Syrup, Topping, Pastry }
public enum CupSize { Small, Medium, Large }

// ---------- Runtime (non-asset) data ----------

[Serializable]
public class Order
{
    public CustomerSO customer;
    public DrinkRecipeSO recipe;
    public CupSize size;
    public float timeLimitSeconds;
    public string dialogue;                           // what the customer says

    // Expected contents, resolved from the recipe
    public List<IngredientSO> Expected()
    {
        var list = new List<IngredientSO>();
        if (recipe.baseIngredient) list.Add(recipe.baseIngredient);
        if (recipe.milk) list.Add(recipe.milk);
        list.AddRange(recipe.syrups);
        list.AddRange(recipe.toppings);
        return list;
    }
}

/// <summary>What the player has actually built so far.</summary>
[Serializable]
public class DrinkState
{
    public CupSize size;
    public List<IngredientSO> added = new();
    public float elapsedSeconds;                      // time spent on this order
}

[Serializable]
public struct OrderResult
{
    public Order order;
    public int playerPoints;
    public int rivalPoints;
    public int tipCents;
    public int priceCents;
    public bool playerWon => playerPoints > rivalPoints;
    public bool timedOut;
}

[Serializable]
public class DaySummary
{
    public int day;
    public int ordersServed;
    public int ordersFailed;
    public int playerPoints;
    public int rivalPoints;
    public int moneyCents;
    public float takeover;
}
