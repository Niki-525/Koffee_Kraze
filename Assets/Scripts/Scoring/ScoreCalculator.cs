// Scripts/Scoring/ScoreCalculator.cs
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pure, deterministic scoring. No MonoBehaviour, no randomness, so it is
/// trivial to unit test and tune.
///
/// Points (0..100) = 75% accuracy + 25% speed, scaled by a size penalty.
/// </summary>
public static class ScoreCalculator
{
    public const float AccuracyWeight = 0.75f;
    public const float SpeedWeight = 0.25f;
    public const float WrongSizeMultiplier = 0.8f;
    public const float ExtraIngredientPenalty = 0.5f;  // each extra counts as half a miss

    public static int PriceFor(Order order)
    {
        float sizeMult = order.size switch
        {
            CupSize.Small => 0.85f,
            CupSize.Large => 1.25f,
            _ => 1f
        };
        return Mathf.RoundToInt(order.recipe.basePriceCents * sizeMult);
    }

    /// <summary>Fraction (0..1) of the order that was made correctly.</summary>
    public static float Accuracy(Order order, DrinkState drink)
    {
        var expected = order.Expected();
        if (expected.Count == 0) return 1f;

        // Multiset comparison by ingredient id.
        var remaining = new Dictionary<string, int>();
        foreach (var e in expected)
        {
            remaining.TryGetValue(e.id, out int n);
            remaining[e.id] = n + 1;
        }

        int correct = 0, extra = 0;
        foreach (var a in drink.added)
        {
            if (a != null && remaining.TryGetValue(a.id, out int n) && n > 0)
            {
                remaining[a.id] = n - 1;
                correct++;
            }
            else extra++;
        }

        float score = (correct - ExtraIngredientPenalty * extra) / expected.Count;
        return Mathf.Clamp01(score);
    }

    public static OrderResult Grade(Order order, DrinkState drink, float timeRemaining)
    {
        float accuracy = Accuracy(order, drink);
        float speed = order.timeLimitSeconds > 0f
            ? Mathf.Clamp01(timeRemaining / order.timeLimitSeconds)
            : 0f;

        float raw = AccuracyWeight * accuracy + SpeedWeight * speed;

        // Speed alone shouldn't earn points for a wrong drink.
        if (accuracy <= 0f) raw = 0f;

        if (drink.size != order.size) raw *= WrongSizeMultiplier;

        int points = Mathf.RoundToInt(raw * 100f);
        int price = PriceFor(order);

        // Tip scales with points; only good drinks tip.
        float tipRate = points >= 90 ? 0.25f : points >= 75 ? 0.15f : points >= 50 ? 0.05f : 0f;
        int tip = Mathf.RoundToInt(price * tipRate * order.customer.tipMultiplier);

        return new OrderResult
        {
            order = order,
            playerPoints = points,
            priceCents = price,
            tipCents = tip,
            timedOut = false
        };
    }
}
