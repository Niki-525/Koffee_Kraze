// Scripts/Core/GameManager.cs
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Owns the day loop: pulls orders, runs them through the station pipeline,
/// scores them head-to-head against the rival, and hands the day summary to
/// the rival strategist at day's end.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Setup")]
    [Tooltip("Stations in play order. The LAST one should be the CheckoutStation.")]
    public List<Station> stations = new();
    public OrderManager orders;
    public TakeoverManager takeover;
    public EconomyManager economy;

    [Header("Day")]
    public float dayLengthSeconds = 90f;
    public float strategistTimeoutSeconds = 8f;
    [Tooltip("If true, the next day waits for ContinueToNextDay() (hook to a UI button).")]
    public bool waitForContinueButton = true;

    [Header("Rival strategist")]
    public bool useLlmStrategist = false;
    public LlmStrategistSettings llmSettings;

    public RivalState Rival { get; private set; } = new RivalState();
    public int Day { get; private set; } = 1;
    public Order CurrentOrder { get; private set; }
    public float OrderTimeRemaining { get; private set; }
    public float DayTimeRemaining { get; private set; }

    IRivalStrategist _strategist;
    readonly RuleBasedStrategist _fallback = new RuleBasedStrategist();
    readonly System.Random _rng = new System.Random();
    DaySummary _summary;
    bool _continueRequested;
    bool _gameOver;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnEnable()  { GameEvents.GameOver += OnGameOver; }
    void OnDisable() { GameEvents.GameOver -= OnGameOver; }
    void OnGameOver() => _gameOver = true;

    void Start()
    {
        _strategist = (useLlmStrategist && llmSettings != null)
            ? new LlmStrategist(llmSettings, _fallback)
            : _fallback;

        foreach (var s in stations) s.gameObject.SetActive(false);

        LoadProgress();
        StartCoroutine(RunGame());
    }

    public void ContinueToNextDay() => _continueRequested = true;

    // ------------------------------------------------------------------
    IEnumerator RunGame()
    {
        while (!_gameOver)
        {
            yield return RunDay();
            if (_gameOver) break;
            Day++;
            SaveProgress();
        }
    }

    IEnumerator RunDay()
    {
        _summary = new DaySummary { day = Day };
        DayTimeRemaining = dayLengthSeconds;
        GameEvents.RaiseDayStarted(Day);

        while (DayTimeRemaining > 0f && !_gameOver)
        {
            // Fetch (prefetched) next order.
            Order next = null;
            yield return orders.GetNextOrder(Day, o => next = o);
            if (next == null) { yield return null; continue; }
            orders.PrefetchNext(Day);

            yield return RunOrder(next);
        }

        // ----- end of day -----
        _summary.moneyCents = economy != null ? economy.MoneyCents : 0;
        _summary.takeover = takeover != null ? takeover.Value : 0f;
        GameEvents.RaiseDayEnded(_summary);

        if (_gameOver) yield break;

        yield return ConsultStrategist(_summary);

        if (waitForContinueButton)
        {
            _continueRequested = false;
            while (!_continueRequested) yield return null;
        }
    }

    IEnumerator RunOrder(Order order)
    {
        CurrentOrder = order;
        var drink = new DrinkState { size = order.size };
        OrderTimeRemaining = order.timeLimitSeconds;
        GameEvents.RaiseOrderStarted(order);

        bool timedOut = false;

        foreach (var station in stations)
        {
            bool done = false;
            void OnDone() => done = true;
            station.Completed += OnDone;
            station.Enter(order, drink);

            while (!done)
            {
                float dt = Time.deltaTime;
                OrderTimeRemaining -= dt;
                DayTimeRemaining -= dt;
                drink.elapsedSeconds += dt;

                if (OrderTimeRemaining <= 0f) { timedOut = true; break; }
                yield return null;
            }

            station.Completed -= OnDone;
            station.Exit();
            if (timedOut) break;

            if (station is AssemblyStation) GameEvents.RaiseBuildCompleted(drink);
        }

        // ----- Score head-to-head -----
        OrderResult result;
        if (timedOut)
        {
            result = new OrderResult
            {
                order = order,
                playerPoints = 0,
                timedOut = true,
                priceCents = 0,
                tipCents = 0
            };
            result.rivalPoints = RivalSimulator.Simulate(order, Rival, _rng);
            _summary.ordersFailed++;
            GameEvents.RaiseOrderFailed(order);
        }
        else
        {
            result = ScoreCalculator.Grade(order, drink, Mathf.Max(0f, OrderTimeRemaining));
            result.rivalPoints = RivalSimulator.Simulate(order, Rival, _rng);
            if (economy != null)
            {
                economy.AddMoney(result.tipCents);
                economy.AddPoints(result.playerPoints);
            }
            _summary.ordersServed++;
        }

        _summary.playerPoints += result.playerPoints;
        _summary.rivalPoints += result.rivalPoints;
        Rival.totalPoints += result.rivalPoints;

        CurrentOrder = null;
        GameEvents.RaiseOrderCompleted(result);

        // Short breather between customers.
        yield return new WaitForSeconds(1.5f);
    }

    // ------------------------------------------------------------------
    IEnumerator ConsultStrategist(DaySummary summary)
    {
        var cts = new CancellationTokenSource();
        Task<RivalDecision> task;
        try { task = _strategist.DecideAsync(summary, Rival, cts.Token); }
        catch (Exception e)
        {
            Debug.LogWarning($"Strategist threw: {e.Message}");
            task = Task.FromResult(_fallback.Decide(summary, Rival));
        }

        float t = 0f;
        while (!task.IsCompleted && t < strategistTimeoutSeconds)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        RivalDecision decision;
        if (task.IsCompleted && !task.IsFaulted && !task.IsCanceled && task.Result != null)
        {
            decision = task.Result;
        }
        else
        {
            cts.Cancel();
            decision = _fallback.Decide(summary, Rival);
        }

        Rival.Apply(decision);  // clamps everything
        if (!string.IsNullOrWhiteSpace(decision.line))
            GameEvents.RaiseRivalSpoke(decision.line);
    }

    // ------------------------------------------------------------------
    void SaveProgress()
    {
        var data = new SaveData
        {
            day = Day,
            moneyCents = economy ? economy.MoneyCents : 0,
            totalPoints = economy ? economy.TotalPoints : 0,
            takeover = takeover ? takeover.Value : 0.3f,
            rivalSkill = Rival.skill,
            rivalPriceMultiplier = Rival.priceMultiplier,
            rivalAggression = Rival.aggression
        };
        SaveSystem.Save(data);
    }

    void LoadProgress()
    {
        var data = SaveSystem.Load();
        Day = Mathf.Max(1, data.day);
        if (economy) economy.Load(data.moneyCents, data.totalPoints);
        if (takeover) takeover.SetValue(data.takeover);
        Rival.skill = data.rivalSkill;
        Rival.priceMultiplier = data.rivalPriceMultiplier;
        Rival.aggression = data.rivalAggression;
        Rival.Clamp();
        if (takeover) takeover.rival = Rival;
    }
}
