// Scripts/Core/OrderManager.cs
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>Anything that can produce an order. Random, or LLM-backed later.</summary>
public interface IOrderGenerator
{
    Task<Order> GenerateAsync(int day, List<DrinkRecipeSO> recipes, List<CustomerSO> customers);
}

/// <summary>Offline, data-driven generator. Also the fallback for any AI generator.</summary>
public class RandomOrderGenerator : IOrderGenerator
{
    readonly System.Random _rng = new System.Random();

    public Task<Order> GenerateAsync(int day, List<DrinkRecipeSO> recipes, List<CustomerSO> customers)
    {
        var available = recipes.Where(r => r != null && r.unlockDay <= day).ToList();
        if (available.Count == 0 || customers.Count == 0) return Task.FromResult<Order>(null);

        var recipe = available[_rng.Next(available.Count)];
        var customer = customers[_rng.Next(customers.Count)];
        var size = (CupSize)_rng.Next(3);

        // Time limit shrinks slowly as days progress, floored so it stays fair.
        float baseTime = customer.patienceSeconds;
        float limit = Mathf.Max(12f, baseTime - (day - 1) * 1f);

        return Task.FromResult(new Order
        {
            customer = customer,
            recipe = recipe,
            size = size,
            timeLimitSeconds = limit,
            dialogue = $"One {size.ToString().ToLower()} {recipe.displayName}, please."
        });
    }
}

public class OrderManager : MonoBehaviour
{
    public List<DrinkRecipeSO> recipes = new();
    public List<CustomerSO> customers = new();

    IOrderGenerator _generator = new RandomOrderGenerator();
    Task<Order> _pending;

    /// <summary>Swap in a different generator (e.g. an LLM one). Falls back to random on failure.</summary>
    public void SetGenerator(IOrderGenerator generator) => _generator = generator;

    public void PrefetchNext(int day)
    {
        if (_pending == null || _pending.IsCompleted)
            _pending = SafeGenerate(day);
    }

    /// <summary>Coroutine-friendly: yields until an order is ready, then calls back.</summary>
    public IEnumerator GetNextOrder(int day, Action<Order> onReady)
    {
        if (_pending == null) _pending = SafeGenerate(day);
        while (!_pending.IsCompleted) yield return null;

        Order o = _pending.IsFaulted || _pending.IsCanceled ? null : _pending.Result;
        _pending = null;
        onReady?.Invoke(o);
    }

    async Task<Order> SafeGenerate(int day)
    {
        try
        {
            var order = await _generator.GenerateAsync(day, recipes, customers);
            if (IsValid(order)) return order;
        }
        catch (Exception e) { Debug.LogWarning($"Order generator failed: {e.Message}"); }

        // Fallback so the game never stalls.
        return await new RandomOrderGenerator().GenerateAsync(day, recipes, customers);
    }

    static bool IsValid(Order o) =>
        o != null && o.recipe != null && o.customer != null && o.timeLimitSeconds > 0f;
}
