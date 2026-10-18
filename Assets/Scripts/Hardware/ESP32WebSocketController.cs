using System;
using System.Text;
using UnityEngine;
using NativeWebSocket;
using System.Threading.Tasks;

// ESP32 -> Unity
[Serializable]
public class InMsg {
    public string type;
    public string key;
    public float x, y;
    public bool pressed;
}

// Unity -> ESP32
[Serializable]
public class OutMsg {
    public string type;
    public int[] leds;
    public int ms;
    public string top;   // for LCD top line
    public string bottom; // for LCD bottom line
}

public class ESP32WebSocketController : MonoBehaviour, IRegisterDevice {
    public static ESP32WebSocketController Instance {get; private set;}

    public string url = "ws://192.168.4.1:81";

    public Vector2 JoyStick {get; private set; }
    public bool JoyButton {get; private set; }
    public string KeyPad {get; private set; }

    //define events that can be used in other files
    public event Action<string> OnKey;
    public event Action<bool> OnJoyButton;

    // IRegisterDevice implementation
    public event Action<char> KeyPressed;
    public event Action DrawerButtonPressed;

    public string Name => "ESP32 WebSocket";
    public bool IsConnected => ws != null && ws.State == WebSocketState.Open;

    WebSocket ws;
    private float _reconnectTimer;
    private const float ReconnectDelay = 5f; // seconds

    void Awake(){
        if(Instance !=null ){ Destroy(gameObject); return;}
        Instance = this;
        DontDestroyOnLoad(gameObject);
        _reconnectTimer = 0f;
    }

    async void Start(){
        await ConnectToWebsocket();
    }

    async Task ConnectToWebsocket(){
        ws = new WebSocket(url);
        ws.OnOpen += () => {
            Debug.Log("Controller connected");
            _reconnectTimer = 0f; // reset timer on connect
        };
        ws.OnClose += (code) => {
            Debug.Log("Controller disconnected");
            // Reconnection will be handled in Tick
        };
        ws.OnError += (err) => Debug.LogWarning("WS error:" + err);
        ws.OnMessage += (bytes) => HandleMessage(Encoding.UTF8.GetString(bytes));
        try {
            await ws.Connect();
        }
        catch (Exception e) {
            Debug.LogWarning("[ESP32] connect failed: " + e.Message);
        }
    }

    void HandleMessage(string json){
        InMsg m;
        try {
            m = JsonUtility.FromJson<InMsg>(json);
            Debug.LogWarning("ESP32 raw: " + m);
        }
        catch{Debug.LogWarning("Bad JSON: " + json); return;}

        switch (m.type) {
            case "key":
                KeyPad = m.key;
                OnKey?.Invoke(m.key);
                // Handle IRegisterDevice KeyPressed
                if (!string.IsNullOrEmpty(m.key) && m.key.Length == 1) {
                    char c = m.key[0];
                    if (char.IsDigit(c) || c == 'C' || c == 'E') {
                        KeyPressed?.Invoke(c);
                    }
                }
                break;
            case "joy":
                JoyStick = new Vector2(m.x, m.y);
                break;
            case "btn":
                JoyButton = m.pressed;
                OnJoyButton?.Invoke(m.pressed);
                // TODO: Determine if this button should trigger drawer
                // For now, we assume any button press is not the drawer unless specified.
                // You may want to map a specific button to drawer press.
                // Example: if (m.pressed && /* some condition */) DrawerButtonPressed?.Invoke();
                break;
        }
    }

    // Outgoing
    async void Send(OutMsg m){
        if (ws != null && ws.State == WebSocketState.Open){
            await ws.SendText(JsonUtility.ToJson(m));
        }
    }

    public void SetLeds(params int[] states) {
        Send(new OutMsg { type = "leds", leds = states });
    }

    public void Buzz(int ms) {
        // Note: The original had a bug: params ms should be params int[] ms
        // We keep it as a single int for simplicity; adjust if your protocol expects an array.
        Send(new OutMsg { type = "buzz", ms = ms });
    }

    // IRegisterDevice methods
    public void ShowLines(string top, string bottom) {
        Send(new OutMsg {
            type = "lcd",
            top = top,
            bottom = bottom
        });
    }

    public void OpenDrawer() {
        Send(new OutMsg { type = "drawer" });
    }

    public void Tick()
    {
        // Handle reconnection if needed
        if (ws == null || ws.State == WebSocketState.Closed)
        {
            _reconnectTimer += Time.unscaledDeltaTime;
            if (_reconnectTimer >= ReconnectDelay)
            {
                _reconnectTimer = 0f;
                Debug.Log("[ESP32WebSocket] Attempting to reconnect...");
                ConnectToWebsocket(); // We don't await here to avoid blocking
            }
        }
    }

    public void Dispose() {
        if (ws != null) {
            ws.Close();
        }
    }

    void Update(){
        #if !UNITY_WEBGL || UNITY_EDITOR
            ws?.DispatchMessageQueue();
        #endif
    }

    async void OnApplicationQuit(){
        if (ws != null) await ws.Close();
    }
}