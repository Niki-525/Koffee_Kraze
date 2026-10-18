// Scripts/Hardware/RegisterManager.cs
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The only thing the rest of the game talks to for register input/output.
///
/// The keyboard device is always active (great for development and as a
/// fallback). If Mode allows, a serial hardware register is added alongside
/// it, and reconnection is retried automatically if it gets unplugged.
///
/// Entry is in cents, like a real register: typing 4 5 0 gives 450 ($4.50).
/// </summary>
public class RegisterManager : MonoBehaviour
{
    public static RegisterManager Instance { get; private set; }

    public enum Mode { KeyboardOnly, SerialAndKeyboard }

    [Header("Devices")]
    public Mode mode = Mode.SerialAndKeyboard;
    [Tooltip("e.g. COM3 or /dev/ttyUSB0. Leave empty to use the first available port.")]
    public string serialPortName = "";
    public int baudRate = 115200;
    public float reconnectSeconds = 5f;

    [Header("Entry")]
    public int maxEntryDigits = 6;

    public event Action<int> EntryChanged;     // live value in cents
    public event Action<int> EntrySubmitted;   // ENTER pressed, value in cents
    public event Action DrawerPressed;

    public int Entry { get; private set; }

    public bool HardwareConnected
    {
        get
        {
            foreach (var d in _devices)
                if (!(d is KeyboardRegisterDevice) && d.IsConnected) return true;
            return false;
        }
    }

    readonly List<IRegisterDevice> _devices = new List<IRegisterDevice>();
    IRegisterDevice _serial;
    float _reconnectTimer;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        AddDevice(new KeyboardRegisterDevice());
        if (mode == Mode.SerialAndKeyboard) TryConnectSerial();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        foreach (var d in _devices) { Unhook(d); d.Dispose(); }
        _devices.Clear();
    }

    void Update()
    {
        // Index loop: devices may be removed during iteration.
        for (int i = _devices.Count - 1; i >= 0; i--) _devices[i].Tick();

        if (mode != Mode.SerialAndKeyboard) return;

        // Dropped connection? Fall back to keyboard and keep trying.
        if (_serial != null && !_serial.IsConnected)
        {
            Debug.LogWarning($"[Register] {_serial.Name} lost. Keyboard fallback active; will retry.");
            RemoveDevice(_serial);
            _serial = null;
            _reconnectTimer = 0f;
        }

        if (_serial == null)
        {
            _reconnectTimer += Time.unscaledDeltaTime;
            if (_reconnectTimer >= reconnectSeconds)
            {
                _reconnectTimer = 0f;
                TryConnectSerial();
            }
        }
    }

    // ------------------------------------------------------------------
    // Public API used by stations
    // ------------------------------------------------------------------
    public void ClearEntry()
    {
        Entry = 0;
        EntryChanged?.Invoke(Entry);
    }

    public void ShowLines(string top, string bottom)
    {
        foreach (var d in _devices) d.ShowLines(top, bottom);
    }

    public void OpenDrawer()
    {
        foreach (var d in _devices) d.OpenDrawer();
    }

    /// <summary>Feed a key from anywhere (e.g. on-screen keypad buttons).</summary>
    public void InjectKey(char c) => HandleKey(c);

    public void InjectDrawerPress() => DrawerPressed?.Invoke();

    // ------------------------------------------------------------------
    void TryConnectSerial()
    {
#if UNITY_STANDALONE || UNITY_EDITOR
        var dev = new SerialRegisterDevice(serialPortName, baudRate);
        if (dev.TryOpen())
        {
            _serial = dev;
            AddDevice(dev);
            Debug.Log($"[Register] Hardware connected: {dev.Name}");
        }
        else
        {
            dev.Dispose();
        }
#endif
    }

    void AddDevice(IRegisterDevice d)
    {
        _devices.Add(d);
        d.KeyPressed += HandleKey;
        d.DrawerButtonPressed += HandleDrawer;
    }

    void RemoveDevice(IRegisterDevice d)
    {
        Unhook(d);
        _devices.Remove(d);
        d.Dispose();
    }

    void Unhook(IRegisterDevice d)
    {
        d.KeyPressed -= HandleKey;
        d.DrawerButtonPressed -= HandleDrawer;
    }

    void HandleDrawer() => DrawerPressed?.Invoke();

    void HandleKey(char c)
    {
        if (c >= '0' && c <= '9')
        {
            int limit = (int)Math.Pow(10, Mathf.Max(1, maxEntryDigits) - 1);
            if (Entry >= limit) return;          // already at max digits
            Entry = Entry * 10 + (c - '0');
            EntryChanged?.Invoke(Entry);
        }
        else if (c == 'C')
        {
            ClearEntry();
        }
        else if (c == 'E')
        {
            EntrySubmitted?.Invoke(Entry);
        }
    }
}
