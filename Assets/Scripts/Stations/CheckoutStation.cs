// Scripts/Stations/CheckoutStation.cs
using TMPro;
using UnityEngine;

/// <summary>
/// Cash register minigame. The customer pays with a bill; the player keys in
/// the correct change on the register (hardware, keyboard, or on-screen
/// keypad) and presses ENTER. Entry is in cents, like a real register:
/// typing 5 5 0 means $5.50.
/// </summary>
public class CheckoutStation : Station
{
    [Header("UI")]
    public TMP_Text infoText;
    public TMP_Text entryText;
    public TMP_Text feedbackText;

    [Header("Rules")]
    public int wrongAttemptPenaltyCents = 25;
    [Tooltip("If true, after correct change the player must press the DRAWER button to finish.")]
    public bool requireDrawerConfirm = false;

    readonly System.Random _rng = new System.Random();
    int _price, _paid, _changeDue, _wrongAttempts, _collected;
    bool _awaitingDrawer;

    protected override void OnEnter()
    {
        _price = ScoreCalculator.PriceFor(Order);
        _paid = PickTendered(_price);
        _changeDue = _paid - _price;
        _wrongAttempts = 0;
        _awaitingDrawer = false;

        if (infoText)
            infoText.text = $"Total: {EconomyManager.Format(_price)}\nCustomer paid: {EconomyManager.Format(_paid)}\nEnter the change, then press ENTER.";
        if (entryText) entryText.text = EconomyManager.Format(0);
        if (feedbackText) feedbackText.text = "";

        var reg = RegisterManager.Instance;
        if (reg == null)
        {
            Debug.LogWarning("CheckoutStation: no RegisterManager in the scene. Add one so the player can ring up orders.");
            return;
        }

        reg.ClearEntry();
        reg.EntryChanged += OnEntryChanged;
        reg.EntrySubmitted += OnEntrySubmitted;
        reg.DrawerPressed += OnDrawerPressed;
        reg.ShowLines($"TOTAL {EconomyManager.Format(_price)}", $"PAID  {EconomyManager.Format(_paid)}");
    }

    protected override void OnExit()
    {
        var reg = RegisterManager.Instance;
        if (reg == null) return;
        reg.EntryChanged -= OnEntryChanged;
        reg.EntrySubmitted -= OnEntrySubmitted;
        reg.DrawerPressed -= OnDrawerPressed;
    }

    void OnEntryChanged(int cents)
    {
        if (entryText) entryText.text = EconomyManager.Format(cents);
        RegisterManager.Instance?.ShowLines("CHANGE DUE?", EconomyManager.Format(cents));
    }

    void OnEntrySubmitted(int cents)
    {
        if (_awaitingDrawer) return;

        if (cents == _changeDue)
        {
            _collected = Mathf.Max(0, _price - _wrongAttempts * wrongAttemptPenaltyCents);
            RegisterManager.Instance?.OpenDrawer();
            RegisterManager.Instance?.ShowLines("THANK YOU", EconomyManager.Format(_collected));

            if (requireDrawerConfirm)
            {
                _awaitingDrawer = true;
                if (feedbackText) feedbackText.text = "Correct! Hand over the change, then press DRAWER.";
            }
            else
            {
                Finish();
            }
        }
        else
        {
            _wrongAttempts++;
            if (feedbackText)
                feedbackText.text = $"Wrong change! (-{EconomyManager.Format(wrongAttemptPenaltyCents)} each mistake)";
            RegisterManager.Instance?.ClearEntry();
            RegisterManager.Instance?.ShowLines("WRONG CHANGE", "TRY AGAIN");
        }
    }

    void OnDrawerPressed()
    {
        if (_awaitingDrawer) Finish();
    }

    void Finish()
    {
        _awaitingDrawer = false;
        GameEvents.RaiseCheckoutCompleted(_collected);
        Complete();
    }

    // Customer pays with an exact-dollar amount, the next $5, or the next $10.
    int PickTendered(int price)
    {
        int[] options =
        {
            Mathf.CeilToInt(price / 100f) * 100,
            Mathf.CeilToInt(price / 500f) * 500,
            Mathf.CeilToInt(price / 1000f) * 1000
        };
        return options[_rng.Next(options.Length)];
    }
}
