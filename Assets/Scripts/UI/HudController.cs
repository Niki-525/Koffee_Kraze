// Scripts/UI/HudController.cs
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Pure view layer: subscribes to GameEvents and updates the HUD.
/// Assign whichever fields you have; everything is optional.
/// </summary>
public class HudController : MonoBehaviour
{
    [Header("Stats")]
    public TMP_Text moneyText;
    public TMP_Text pointsText;
    public TMP_Text dayText;
    public TMP_Text dayTimerText;
    public Slider takeoverSlider;

    [Header("Rival")]
    public TMP_Text rivalSpeechText;
    public TMP_Text lastResultText;

    [Header("Panels")]
    public GameObject daySummaryPanel;
    public TMP_Text daySummaryText;
    public Button continueButton;
    public GameObject buyoutPanel;
    public GameObject gameOverPanel;

    void Start()
    {
        if (continueButton) continueButton.onClick.AddListener(OnContinue);
        if (daySummaryPanel) daySummaryPanel.SetActive(false);
        if (buyoutPanel) buyoutPanel.SetActive(false);
        if (gameOverPanel) gameOverPanel.SetActive(false);
        RefreshStats();
    }

    void OnEnable()
    {
        GameEvents.DayStarted += OnDayStarted;
        GameEvents.DayEnded += OnDayEnded;
        GameEvents.OrderCompleted += OnOrderCompleted;
        GameEvents.CheckoutCompleted += OnCheckoutCompleted;
        GameEvents.TakeoverChanged += OnTakeoverChanged;
        GameEvents.RivalSpoke += OnRivalSpoke;
        GameEvents.BuyoutOffered += OnBuyoutOffered;
        GameEvents.GameOver += OnGameOver;
    }

    void OnDisable()
    {
        GameEvents.DayStarted -= OnDayStarted;
        GameEvents.DayEnded -= OnDayEnded;
        GameEvents.OrderCompleted -= OnOrderCompleted;
        GameEvents.CheckoutCompleted -= OnCheckoutCompleted;
        GameEvents.TakeoverChanged -= OnTakeoverChanged;
        GameEvents.RivalSpoke -= OnRivalSpoke;
        GameEvents.BuyoutOffered -= OnBuyoutOffered;
        GameEvents.GameOver -= OnGameOver;
    }

    void OnCheckoutCompleted(int cents) => RefreshStats();

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm != null && dayTimerText != null)
            dayTimerText.text = $"{Mathf.CeilToInt(Mathf.Max(0f, gm.DayTimeRemaining))}s left today";
    }

    void RefreshStats()
    {
        var eco = EconomyManager.Instance;
        if (eco == null) return;
        if (moneyText) moneyText.text = EconomyManager.Format(eco.MoneyCents);
        if (pointsText) pointsText.text = $"{eco.TotalPoints} pts";
    }

    void OnDayStarted(int day)
    {
        if (dayText) dayText.text = $"Day {day}";
        if (daySummaryPanel) daySummaryPanel.SetActive(false);
        RefreshStats();
    }

    void OnDayEnded(DaySummary s)
    {
        if (daySummaryText)
            daySummaryText.text =
                $"Day {s.day} complete\n" +
                $"Served: {s.ordersServed}   Failed: {s.ordersFailed}\n" +
                $"You: {s.playerPoints} pts   Rival: {s.rivalPoints} pts\n" +
                $"Takeover risk: {Mathf.RoundToInt(s.takeover * 100)}%";
        if (daySummaryPanel) daySummaryPanel.SetActive(true);
    }

    void OnOrderCompleted(OrderResult r)
    {
        RefreshStats();
        if (lastResultText == null) return;

        lastResultText.text = r.timedOut
            ? $"Order abandoned. Rival: {r.rivalPoints}"
            : $"You {r.playerPoints}  vs  Rival {r.rivalPoints}  {(r.playerWon ? "(you win!)" : "(rival wins)")}";
    }

    void OnTakeoverChanged(float v)
    {
        if (takeoverSlider) takeoverSlider.value = v;
    }

    void OnRivalSpoke(string line)
    {
        if (rivalSpeechText) rivalSpeechText.text = line;
    }

    void OnBuyoutOffered()
    {
        if (buyoutPanel) buyoutPanel.SetActive(true);
    }

    void OnGameOver()
    {
        if (gameOverPanel) gameOverPanel.SetActive(true);
    }

    void OnContinue()
    {
        if (daySummaryPanel) daySummaryPanel.SetActive(false);
        if (buyoutPanel) buyoutPanel.SetActive(false);
        GameManager.Instance?.ContinueToNextDay();
    }
}
