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

            BeginRequest();
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
            if (string.IsNullOrEmpty(apiKey)) return;

            string inputMessage = playerInputField.text;
            if (string.IsNullOrWhiteSpace(inputMessage)) return;

            playerInputField.text = "";
            conversationHistory.Add(new ContentTurn("user", inputMessage));

            BeginRequest();
        }

        private void OnOrderStarted(Order order)
        {
            if (string.IsNullOrEmpty(apiKey)) return;

            string orderDetails = order == null
                ? "A new customer order has started."
                : $"A new {order.size} drink order has started with {order.timeLimitSeconds:0} seconds on the clock.";

            conversationHistory.Add(new ContentTurn(
                "user",
                $"{orderDetails} Give me a fresh, brief boss remark about this order. Do not repeat your previous line."));
            PublishBossDialogue("<b>Boss:</b> \"Analyzing this order...\"");
            BeginRequest();
        }

        private void BeginRequest()
        {
            if (activeRequest != null)
            {
                StopCoroutine(activeRequest);
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

                yield return webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    string rawJsonResponse = webRequest.downloadHandler.text;
                    ProcessTextResponse(rawJsonResponse);
                }
                else
                {
                    string errorDetails = webRequest.downloadHandler.text;
                    Debug.LogError($"[Gemini Error] {webRequest.error}\n{errorDetails}");
                    PublishBossDialogue($"Rival: AI request failed.\n{errorDetails}");
                }
            }
        }

        private void ProcessTextResponse(string rawJson)
        {
            try
            {
                GeminiResponsePayload responseObj = JsonUtility.FromJson<GeminiResponsePayload>(rawJson);
                if (responseObj != null && responseObj.candidates != null && responseObj.candidates.Count > 0)
                {
                    string dialogueText = responseObj.candidates[0].content.parts[0].text.Trim();

                    // Append model's response back to dialogue context history
                    conversationHistory.Add(new ContentTurn("model", dialogueText));

                    PublishBossDialogue($"<b>Boss:</b> \"{dialogueText}\"");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Parsing Error] Could not process text response: {ex.Message}");
            }
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