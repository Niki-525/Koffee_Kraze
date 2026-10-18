// Scripts/Core/EconomyManager.cs
using UnityEngine;

public class EconomyManager : MonoBehaviour
{
    public static EconomyManager Instance { get; private set; }

    public int MoneyCents { get; private set; }
    public int TotalPoints { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnEnable()  { GameEvents.CheckoutCompleted += OnCheckout; }
    void OnDisable() { GameEvents.CheckoutCompleted -= OnCheckout; }

    void OnCheckout(int cents) => AddMoney(cents);

    public void AddMoney(int cents) => MoneyCents += cents;

    public bool TrySpend(int cents)
    {
        if (cents > MoneyCents) return false;
        MoneyCents -= cents;
        return true;
    }

    public void AddPoints(int points) => TotalPoints += points;

    public void Load(int money, int points) { MoneyCents = money; TotalPoints = points; }
    public static string Format(int cents) => $"${cents / 100}.{Mathf.Abs(cents % 100):00}";
}
