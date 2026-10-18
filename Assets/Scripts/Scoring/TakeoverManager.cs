// Scripts/Scoring/TakeoverManager.cs
using UnityEngine;

/// <summary>
/// Owns the takeover meter (0 = safe, 1 = bought out).
/// Listens only to order events, so nothing else needs to know it exists.
/// </summary>
public class TakeoverManager : MonoBehaviour
{
    [Range(0f, 1f)] public float startValue = 0.3f;

    [Header("Tuning")]
    public float buyoutOfferThreshold = 0.75f;
    public float buyoutResetThreshold = 0.6f;
    public float minPressure = 0.03f;      // per-order swing at aggression 0
    public float maxPressure = 0.08f;      // per-order swing at aggression 1
    public float playerPointsRelief = 0.02f;
    public float timeoutPenalty = 0.06f;

    [HideInInspector] public RivalState rival;   // set by GameManager

    public float Value { get; private set; }
    bool _offerActive;

    void Awake() => Value = startValue;

    void OnEnable()
    {
        GameEvents.OrderCompleted += OnOrderCompleted;
    }

    void OnDisable()
    {
        GameEvents.OrderCompleted -= OnOrderCompleted;
    }

    public void SetValue(float v)
    {
        Value = Mathf.Clamp01(v);
        GameEvents.RaiseTakeoverChanged(Value);
    }

    void OnOrderCompleted(OrderResult r)
    {
        float aggression = rival != null ? rival.aggression : 0.5f;
        float pressure = Mathf.Lerp(minPressure, maxPressure, aggression);

        // Positive margin = rival ahead = meter goes up.
        float margin = (r.rivalPoints - r.playerPoints) / 100f;
        float delta = margin * pressure;

        // Good service always helps a little, regardless of the rival.
        delta -= (r.playerPoints / 100f) * playerPointsRelief;

        // Abandoned orders hand the rival free ground.
        if (r.timedOut) delta += timeoutPenalty * (0.5f + aggression);

        Apply(delta);
    }

    void Apply(float delta)
    {
        Value = Mathf.Clamp01(Value + delta);
        GameEvents.RaiseTakeoverChanged(Value);

        if (Value >= 1f)
        {
            ESP32WebSocketController.Instance.Buzz(500);
            GameEvents.RaiseGameOver();
            return;
        }

        if (!_offerActive && Value >= buyoutOfferThreshold)
        {
            _offerActive = true;
            ESP32WebSocketController.Instance.Buzz(500);
            GameEvents.RaiseBuyoutOffered();
        }
        else if (_offerActive && Value < buyoutResetThreshold)
        {
            _offerActive = false;
        }
    }
}
