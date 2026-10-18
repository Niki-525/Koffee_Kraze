// Scripts/Rival/RivalCore.cs
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>Tunable parameters of the rival shop. The strategist nudges these.</summary>
[Serializable]
public class RivalState
{
    public float skill = 0.5f;            // 0.2..0.95  drives per-order score
    public float priceMultiplier = 1f;    // 0.7..1.3   undercutting = small point bonus
    public float aggression = 0.5f;       // 0.1..1     scales takeover pressure
    public int totalPoints;

    public const float MaxDelta = 0.1f;   // largest change one decision may make

    public void Clamp()
    {
        skill = Mathf.Clamp(skill, 0.2f, 0.95f);
        priceMultiplier = Mathf.Clamp(priceMultiplier, 0.7f, 1.3f);
        aggression = Mathf.Clamp(aggression, 0.1f, 1f);
    }

    /// <summary>
    /// Applies a strategist decision. Every value is bounded, so a bad or
    /// adversarial LLM response can never make the game unwinnable.
    /// </summary>
    public void Apply(RivalDecision d)
    {
        if (d == null) return;
        skill += Mathf.Clamp(Safe(d.skillDelta), -MaxDelta, MaxDelta);
        priceMultiplier += Mathf.Clamp(Safe(d.priceMultiplierDelta), -MaxDelta, MaxDelta);
        aggression += Mathf.Clamp(Safe(d.aggressionDelta), -MaxDelta, MaxDelta);
        Clamp();
    }

    static float Safe(float v) => float.IsNaN(v) || float.IsInfinity(v) ? 0f : v;
}

[Serializable]
public class RivalDecision
{
    public float skillDelta;
    public float priceMultiplierDelta;
    public float aggressionDelta;
    public string line;                   // in-character dialogue shown to the player
}

public interface IRivalStrategist
{
    Task<RivalDecision> DecideAsync(DaySummary summary, RivalState state, CancellationToken ct);
}

/// <summary>
/// Pure rival simulation: how well would the rival have done on this order?
/// Takes the RNG as a parameter so it is deterministic under test.
/// </summary>
public static class RivalSimulator
{
    public static int Simulate(Order order, RivalState rival, System.Random rng)
    {
        float baseline = rival.skill * 100f;

        // Complex drinks are harder, but less so for a highly skilled rival.
        int ingredientCount = order.Expected().Count;
        float complexityPenalty = Mathf.Max(0, ingredientCount - 2) * 3f * (1f - rival.skill);

        // Undercutting on price wins customers: small bonus; overpricing, small penalty.
        float priceBonus = (1f - rival.priceMultiplier) * 10f;

        float noise = Gaussian(rng) * 8f;

        float points = baseline - complexityPenalty + priceBonus + noise;
        return Mathf.Clamp(Mathf.RoundToInt(points), 0, 100);
    }

    // Box-Muller transform.
    static float Gaussian(System.Random rng)
    {
        double u1 = 1.0 - rng.NextDouble();
        double u2 = rng.NextDouble();
        return (float)(Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2));
    }
}

/// <summary>Offline strategist. Also the fallback and test harness for the LLM one.</summary>
public class RuleBasedStrategist : IRivalStrategist
{
    public Task<RivalDecision> DecideAsync(DaySummary summary, RivalState state, CancellationToken ct)
        => Task.FromResult(Decide(summary, state));

    public RivalDecision Decide(DaySummary s, RivalState state)
    {
        var d = new RivalDecision();
        int diff = s.playerPoints - s.rivalPoints;   // positive: player is ahead

        if (s.ordersServed + s.ordersFailed == 0)
        {
            d.line = "Quiet day? Efficiency says that's a human problem.";
            return d;
        }

        if (diff > 40)
        {
            // Player dominated: rival adapts hard.
            d.skillDelta = 0.06f;
            d.aggressionDelta = 0.08f;
            d.priceMultiplierDelta = -0.05f;
            d.line = "Recalibrating. My models just learned a great deal from you.";
        }
        else if (diff > 0)
        {
            d.skillDelta = 0.03f;
            d.aggressionDelta = 0.03f;
            d.line = "A narrow loss. Not a mistake I will repeat.";
        }
        else if (diff > -40)
        {
            d.skillDelta = 0.01f;
            d.priceMultiplierDelta = 0.02f;
            d.line = "Close. But 'close' is not a business model.";
        }
        else
        {
            // Rival is crushing it: ease off slightly so the game stays winnable.
            d.skillDelta = -0.02f;
            d.aggressionDelta = 0.02f;
            d.line = "Your shop is charming. It will make a lovely archive photo.";
        }

        if (s.ordersFailed >= 2)
        {
            d.aggressionDelta += 0.04f;
            d.line = "I noticed the abandoned orders. Customers remember.";
        }

        return d;
    }
}
