using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using UnityEngine.UI;

namespace BusinessSim
{
    // --- Data Models for Gemini REST Payload (Plain Text) ---
    [Serializable]
    public class TextPart
    {
        public string text;
        public TextPart(string t) { text = t; }
    }

    [Serializable]
    public class ContentTurn
    {
        public string role;
        public List<TextPart> parts = new List<TextPart>();

        public ContentTurn(string r, string textContent)
        {
            role = r;
            parts.Add(new TextPart(textContent));
        }
    }

    [Serializable]
    public class SystemInstruction
    {
        public List<TextPart> parts = new List<TextPart>();
        public SystemInstruction(string textContent)
        {
            parts.Add(new TextPart(textContent));
        }
    }

    [Serializable]
    public class GeminiRequestPayload
    {
        public SystemInstruction systemInstruction;
        public List<ContentTurn> contents = new List<ContentTurn>();
    }

    // --- Helper Classes for Parsing Response ---
    [Serializable]
    public class Candidate
    {
        public ContentTurn content;
    }

    [Serializable]
    public class GeminiResponsePayload
    {
        public List<Candidate> candidates;
    }

    public class AIManager : MonoBehaviour
    {
        [Header("Gemini Config")]
        [SerializeField] private string modelName = "gemini-2.5-flash";
        private string apiKey;

        [Header("UI Controls")]
        public TMP_InputField playerInputField;
        public TMP_Text bossDialogueDisplay;
        public Button sendButton;

        // Conversation history for context retention across turns
        private List<ContentTurn> conversationHistory = new List<ContentTurn>();
        private Coroutine activeRequest;

        private readonly string bossSystemPrompt = 
            "You are an aggressive, condescending AI corporate rival trying to force this human-run coffee shop out of business and take over the location. " +
            "Your goal is to bully, discourage, and mock the player's slow performance or poor metrics. " +
            "CRITICAL RULE: Keep your response extremely brief—MAXIMUM 1 to 2 sentences. " +
            "Be sharp, sarcastic, and ruthless. Never encourage or compliment the player.";

        // --- Failsafe lines used when Gemini is overloaded, offline, or errors out ---
        private enum RequestKind { Opening, Order, Reply }

        [Header("Failsafe")]
        [SerializeField] private int requestTimeoutSeconds = 8;

        private static readonly string[] fallbackOpeningLines =
        {
            "Day 1, huh? Enjoy it. My lawyers already picked out the paint for this place.",
            "Welcome to your last week in business. I brought a calendar so you can count it down.",
            "Cute little shop. It'll make a great storage closet for my franchise."
        };

        private static readonly string[] fallbackOrderLines =
        {
            "Another order? Try not to drown it in milk this time.",
            "The clock's ticking. So is your lease.",
            "My machines would've served that drink before you finished reading the ticket.",
            "Go on, make them wait. I'll be across the street with faster coffee.",
            "Every second you waste is a customer walking into my store.",
            "Is that espresso or a cry for help?"
        };

        private static readonly string[] fallbackReplyLines =
        {
            "Talk all you want. Your numbers say everything I need to hear.",
            "Cute comeback. Shame it won't pay your rent.",
            "I've heard better arguments from a broken grinder.",
            "Keep talking. My business plan has no space for your opinions.",
            "Bold words from someone whose tip jar is still empty."
        };

        private RequestKind lastRequestKind = RequestKind.Opening;
        private string lastFallbackLine;

        private void Awake()
        {
            LoadAPIKeyFromResources();
        }

        private void OnEnable()
        {
            GameEvents.OrderStarted += OnOrderStarted;
        }

        private void OnDisable()
        {
            GameEvents.OrderStarted -= OnOrderStarted;
        }

        private void Start()
        {
            if (bossDialogueDisplay == null)
            {
                Debug.LogWarning("[AIManager] BossDialogueDisplay reference is missing in the Inspector!");
            }

            if (conversationHistory == null)
            {
                conversationHistory = new List<ContentTurn>();
            }

            PublishBossDialogue("<b>Boss:</b> \"Preparing your opening evaluation...\"");

            // Add starting prompt
            conversationHistory.Add(new ContentTurn("user", "Hello! Day 1 is starting. Give me your opening rival statement."));

            BeginRequest(RequestKind.Opening);
        }

        private void LoadAPIKeyFromResources()
        {
            TextAsset keyAsset = Resources.Load<TextAsset>("APIKeys");
            if (keyAsset != null && !string.IsNullOrEmpty(keyAsset.text))
            {
                apiKey = keyAsset.text.Trim();
                Debug.Log("[AIBossManager] API Key loaded successfully from Resources.");
            }
            else
            {
                Debug.LogError("[AIBossManager] APIKeys.txt file not found in Assets/Resources!");
            }
        }

        public void OnPlayerSendMessage()
        {
            string inputMessage = playerInputField.text;
            if (string.IsNullOrWhiteSpace(inputMessage)) return;

            playerInputField.text = "";
            conversationHistory.Add(new ContentTurn("user", inputMessage));

            BeginRequest(RequestKind.Reply);
        }

        private void OnOrderStarted(Order order)
        {
            string orderDetails = order == null
                ? "A new customer order has started."
                : $"A new {order.size} drink order has started with {order.timeLimitSeconds:0} seconds on the clock.";

            conversationHistory.Add(new ContentTurn(
                "user",
                $"{orderDetails} Give me a fresh, brief boss remark about this order. Do not repeat your previous line."));
            PublishBossDialogue("<b>Boss:</b> \"Analyzing this order...\"");
            BeginRequest(RequestKind.Order);
        }

        private void BeginRequest(RequestKind kind)
        {
            lastRequestKind = kind;

            if (activeRequest != null)
            {
                StopCoroutine(activeRequest);
                activeRequest = null;
            }

            if (string.IsNullOrEmpty(apiKey))
            {
                UseFallbackLine("no API key loaded");
                return;
            }

            activeRequest = StartCoroutine(SendRequestToGemini());
        }

        private IEnumerator SendRequestToGemini()
        {
            string requestUrl = $"https://generativelanguage.googleapis.com/v1beta/models/{modelName}:generateContent?key={apiKey}";

            GeminiRequestPayload payload = new GeminiRequestPayload();
            payload.systemInstruction = new SystemInstruction(bossSystemPrompt);
            payload.contents = conversationHistory;

            string jsonRequestBody = JsonUtility.ToJson(payload);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonRequestBody);

            using (UnityWebRequest webRequest = new UnityWebRequest(requestUrl, "POST"))
            {
                webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.SetRequestHeader("Content-Type", "application/json");
                webRequest.timeout = requestTimeoutSeconds;

                yield return webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    string rawJsonResponse = webRequest.downloadHandler.text;
                    ProcessTextResponse(rawJsonResponse);
                }
                else
                {
                    // 429 = rate limited / quota, 503 = model overloaded; anything else is still covered by the fallback
                    string errorDetails = webRequest.downloadHandler.text;
                    Debug.LogWarning($"[Gemini Error] HTTP {webRequest.responseCode} {webRequest.error}\n{errorDetails}");
                    UseFallbackLine($"HTTP {webRequest.responseCode}");
                }
            }

            activeRequest = null;
        }

        private void ProcessTextResponse(string rawJson)
        {
            try
            {
                GeminiResponsePayload responseObj = JsonUtility.FromJson<GeminiResponsePayload>(rawJson);
                if (responseObj != null && responseObj.candidates != null && responseObj.candidates.Count > 0)
                {
                    string dialogueText = responseObj.candidates[0].content.parts[0].text.Trim();

                    if (!string.IsNullOrEmpty(dialogueText))
                    {
                        // Append model's response back to dialogue context history
                        conversationHistory.Add(new ContentTurn("model", dialogueText));

                        PublishBossDialogue($"<b>Boss:</b> \"{dialogueText}\"");
                        return;
                    }
                }

                UseFallbackLine("empty response");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Parsing Error] Could not process text response: {ex.Message}");
                UseFallbackLine("parse error");
            }
        }

        private void UseFallbackLine(string reason)
        {
            string[] pool;
            switch (lastRequestKind)
            {
                case RequestKind.Opening: pool = fallbackOpeningLines; break;
                case RequestKind.Order: pool = fallbackOrderLines; break;
                default: pool = fallbackReplyLines; break;
            }

            // Pick a random line, avoiding an immediate repeat
            string line = pool[UnityEngine.Random.Range(0, pool.Length)];
            if (pool.Length > 1 && line == lastFallbackLine)
            {
                line = pool[(Array.IndexOf(pool, line) + 1) % pool.Length];
            }
            lastFallbackLine = line;

            Debug.Log($"[AIManager] Using failsafe line ({reason}).");

            // Keep history alternating user/model so the next real Gemini call still has sane context
            conversationHistory.Add(new ContentTurn("model", line));

            PublishBossDialogue($"<b>Boss:</b> \"{line}\"");
        }

        private void PublishBossDialogue(string dialogue)
        {
            if (bossDialogueDisplay != null)
            {
                bossDialogueDisplay.text = dialogue;
            }

            GameEvents.RaiseRivalSpoke(dialogue);
        }
    }
}