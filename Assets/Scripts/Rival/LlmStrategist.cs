// Scripts/Rival/LlmStrategist.cs
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Settings asset for the LLM-driven rival.
///
/// IMPORTANT: never put a model-provider API key in this asset or anywhere in
/// the game build: it can be extracted. Point endpointUrl at a small backend
/// YOU control; that backend holds the real key, builds the model prompt,
/// calls the model, and returns the JSON described below.
/// </summary>
[CreateAssetMenu(menuName = "Coffee/LLM Strategist Settings")]
public class LlmStrategistSettings : ScriptableObject
{
    [Tooltip("Your backend URL (HTTPS). It receives the day summary and returns a RivalDecision JSON.")]
    public string endpointUrl = "https://your-backend.example.com/rival-decision";

    [Tooltip("Optional token identifying this game to YOUR backend. Not a model-provider key.")]
    public string authToken = "";

    public int timeoutSeconds = 6;

    [TextArea(4, 12)]
    public string systemPrompt =
        "You are the strategist for an AI-run coffee chain trying to buy out the last human-run cafe in town. " +
        "You are confident, a little smug, never cruel. Given yesterday's results, adjust your shop's strategy. " +
        "Stay within small adjustments: each delta must be between -0.1 and 0.1. " +
        "Reply with JSON only, no commentary, in exactly this shape: " +
        "{\"skillDelta\":0.0,\"priceMultiplierDelta\":0.0,\"aggressionDelta\":0.0,\"line\":\"one short in-character sentence\"}";
}

/// <summary>
/// Rival strategist backed by a language model via your own backend.
/// Anything that goes wrong (network, timeout, malformed JSON) falls back to
/// the rule-based strategist, so the game never stalls or breaks.
/// Called once per day at day's end, so latency does not matter to gameplay.
/// </summary>
public class LlmStrategist : IRivalStrategist
{
    const int MaxLineLength = 140;

    readonly LlmStrategistSettings _settings;
    readonly RuleBasedStrategist _fallback;

    public LlmStrategist(LlmStrategistSettings settings, RuleBasedStrategist fallback)
    {
        _settings = settings;
        _fallback = fallback;
    }

    [Serializable]
    class Request
    {
        public string system;
        public int day, ordersServed, ordersFailed, playerPoints, rivalPoints, moneyCents;
        public float takeover;
        public float rivalSkill, rivalPriceMultiplier, rivalAggression;
    }

    // Must be called on the main thread (UnityWebRequest requirement).
    // GameManager does this from a coroutine, so that holds.
    public async Task<RivalDecision> DecideAsync(DaySummary s, RivalState state, CancellationToken ct)
    {
        try
        {
            var payload = new Request
            {
                system = _settings.systemPrompt,
                day = s.day,
                ordersServed = s.ordersServed,
                ordersFailed = s.ordersFailed,
                playerPoints = s.playerPoints,
                rivalPoints = s.rivalPoints,
                moneyCents = s.moneyCents,
                takeover = s.takeover,
                rivalSkill = state.skill,
                rivalPriceMultiplier = state.priceMultiplier,
                rivalAggression = state.aggression
            };

            using (var req = new UnityWebRequest(_settings.endpointUrl, UnityWebRequest.kHttpVerbPOST))
            {
                req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload)));
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                if (!string.IsNullOrEmpty(_settings.authToken))
                    req.SetRequestHeader("Authorization", "Bearer " + _settings.authToken);
                req.timeout = Mathf.Max(1, _settings.timeoutSeconds);

                var op = req.SendWebRequest();
                while (!op.isDone)
                {
                    if (ct.IsCancellationRequested)
                    {
                        req.Abort();
                        return _fallback.Decide(s, state);
                    }
                    await Task.Yield();
                }

                if (req.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"[LlmStrategist] Request failed: {req.error}. Using rule-based fallback.");
                    return _fallback.Decide(s, state);
                }

                var decision = Parse(req.downloadHandler.text);
                if (decision == null)
                {
                    Debug.LogWarning("[LlmStrategist] Could not parse response. Using rule-based fallback.");
                    return _fallback.Decide(s, state);
                }
                return decision;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[LlmStrategist] {e.GetType().Name}: {e.Message}. Using rule-based fallback.");
            return _fallback.Decide(s, state);
        }
    }

    /// <summary>
    /// Extracts the JSON object from the response (tolerating code fences or
    /// stray text around it) and sanitizes it. Returns null if unusable.
    /// RivalState.Apply later clamps the numeric deltas again.
    /// </summary>
    internal static RivalDecision Parse(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;

        int start = raw.IndexOf('{');
        int end = raw.LastIndexOf('}');
        if (start < 0 || end <= start) return null;

        try
        {
            var d = JsonUtility.FromJson<RivalDecision>(raw.Substring(start, end - start + 1));
            if (d == null) return null;

            d.skillDelta = Sanitize(d.skillDelta);
            d.priceMultiplierDelta = Sanitize(d.priceMultiplierDelta);
            d.aggressionDelta = Sanitize(d.aggressionDelta);
            d.line = CleanLine(d.line);
            return d;
        }
        catch (Exception)
        {
            return null;
        }
    }

    static float Sanitize(float v)
    {
        if (float.IsNaN(v) || float.IsInfinity(v)) return 0f;
        return Mathf.Clamp(v, -RivalState.MaxDelta, RivalState.MaxDelta);
    }

    // Single line, no control characters, bounded length: it is shown in the UI.
    static string CleanLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line)) return "";
        var sb = new StringBuilder(line.Length);
        foreach (char c in line)
            sb.Append(char.IsControl(c) ? ' ' : c);
        string cleaned = System.Text.RegularExpressions.Regex.Replace(sb.ToString(), @"\s+", " ").Trim();
        return cleaned.Length > MaxLineLength ? cleaned.Substring(0, MaxLineLength - 1) + "…" : cleaned;
    }
}
