// Scripts/Core/GameEvents.cs
using System;

/// <summary>
/// Central static event hub. Systems publish and subscribe here so nothing
/// needs direct references to anything else.
/// </summary>
public static class GameEvents
{
    // Order lifecycle
    public static event Action<Order> OrderStarted;
    public static event Action<OrderResult> OrderCompleted;
    public static event Action<Order> OrderFailed;          // timed out

    // Station flow
    public static event Action<DrinkState> BuildCompleted;
    public static event Action<int> CheckoutCompleted;      // cents collected

    // Day / meta
    public static event Action<int> DayStarted;
    public static event Action<DaySummary> DayEnded;
    public static event Action<float> TakeoverChanged;      // 0..1
    public static event Action<string> RivalSpoke;          // dialogue line
    public static event Action BuyoutOffered;
    public static event Action GameOver;

    public static void RaiseOrderStarted(Order o) => OrderStarted?.Invoke(o);
    public static void RaiseOrderCompleted(OrderResult r) => OrderCompleted?.Invoke(r);
    public static void RaiseOrderFailed(Order o) => OrderFailed?.Invoke(o);
    public static void RaiseBuildCompleted(DrinkState d) => BuildCompleted?.Invoke(d);
    public static void RaiseCheckoutCompleted(int cents) => CheckoutCompleted?.Invoke(cents);
    public static void RaiseDayStarted(int day) => DayStarted?.Invoke(day);
    public static void RaiseDayEnded(DaySummary s) => DayEnded?.Invoke(s);
    public static void RaiseTakeoverChanged(float v) => TakeoverChanged?.Invoke(v);
    public static void RaiseRivalSpoke(string line) => RivalSpoke?.Invoke(line);
    public static void RaiseBuyoutOffered() => BuyoutOffered?.Invoke();
    public static void RaiseGameOver() => GameOver?.Invoke();
}
