// Scripts/Hardware/SerialRegisterDevice.cs
//
// Requires: Project Settings > Player > Other Settings > Api Compatibility Level
//           = ".NET Framework" (Unity 2021) or ".NET Framework" in Unity 6.
// Desktop builds only (Windows / macOS / Linux). Not for WebGL or mobile.
//
#if UNITY_STANDALONE || UNITY_EDITOR
using System;
using System.Collections.Concurrent;
using System.IO.Ports;
using System.Threading;
using UnityEngine;

/// <summary>
/// Talks to a microcontroller (Arduino / ESP32 / Pico) over USB serial using
/// a simple line-based protocol.
///
/// Device -> Unity:   KEY:5    KEY:C    KEY:E    DRAWER
/// Unity  -> Device:  LCD:top text|bottom text    OPEN
///
/// Reading happens on a background thread and is pushed through a thread-safe
/// queue; everything Unity-facing is raised from Tick() on the main thread.
/// </summary>
public class SerialRegisterDevice : IRegisterDevice
{
    const string DisconnectedMarker = "__DISCONNECTED__";

    readonly string _requestedPort;
    readonly int _baud;
    string _openedPort;

    SerialPort _port;
    Thread _thread;
    volatile bool _running;
    volatile bool _connected;

    readonly ConcurrentQueue<string> _incoming = new ConcurrentQueue<string>();
    readonly object _writeLock = new object();

    public string Name => $"Serial({_openedPort ?? _requestedPort ?? "?"})";
    public bool IsConnected => _connected;

    public event Action<char> KeyPressed;
    public event Action DrawerButtonPressed;

    /// <param name="portName">e.g. "COM3" or "/dev/ttyUSB0". Empty = first available port.</param>
    public SerialRegisterDevice(string portName, int baudRate = 115200)
    {
        _requestedPort = portName;
        _baud = baudRate;
    }

    public static string[] AvailablePorts()
    {
        try { return SerialPort.GetPortNames(); }
        catch { return new string[0]; }
    }

    public bool TryOpen()
    {
        try
        {
            string name = _requestedPort;
            if (string.IsNullOrWhiteSpace(name))
            {
                var ports = AvailablePorts();
                if (ports.Length == 0) return false;
                name = ports[0];
            }

            _port = new SerialPort(name, _baud)
            {
                ReadTimeout = 100,
                WriteTimeout = 200,
                NewLine = "\n",
                DtrEnable = true,
                RtsEnable = true
            };
            _port.Open();

            _openedPort = name;
            _connected = true;
            _running = true;
            _thread = new Thread(ReadLoop) { IsBackground = true, Name = "RegisterSerialReader" };
            _thread.Start();
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Register] Could not open serial port: {e.Message}");
            Cleanup();
            return false;
        }
    }

    // ---------------- background thread ----------------
    void ReadLoop()
    {
        while (_running)
        {
            try
            {
                string line = _port.ReadLine();
                if (!string.IsNullOrEmpty(line)) _incoming.Enqueue(line.Trim());
            }
            catch (TimeoutException) { /* normal: no data this tick */ }
            catch (Exception)
            {
                // Unplugged or port error.
                _connected = false;
                _running = false;
                _incoming.Enqueue(DisconnectedMarker);
            }
        }
    }

    // ---------------- main thread ----------------
    public void Tick()
    {
        while (_incoming.TryDequeue(out string line))
        {
            if (line == DisconnectedMarker)
            {
                Debug.LogWarning("[Register] Serial device disconnected.");
            }
            else if (line == "DRAWER")
            {
                DrawerButtonPressed?.Invoke();
            }
            else if (line.StartsWith("KEY:") && line.Length >= 5)
            {
                char c = char.ToUpperInvariant(line[4]);
                if (char.IsDigit(c) || c == 'C' || c == 'E')
                    KeyPressed?.Invoke(c);
            }
            // Unknown lines (e.g. boot banners) are ignored.
        }
    }

    public void ShowLines(string top, string bottom)
        => Send($"LCD:{Sanitize(top)}|{Sanitize(bottom)}");

    public void OpenDrawer() => Send("OPEN");

    void Send(string text)
    {
        if (!_connected || _port == null) return;
        try
        {
            lock (_writeLock) { _port.WriteLine(text); }
        }
        catch (Exception)
        {
            _connected = false;
            _running = false;
        }
    }

    static string Sanitize(string s)
        => (s ?? "").Replace("|", "/").Replace("\n", " ").Replace("\r", " ");

    public void Dispose()
    {
        _running = false;
        _connected = false;
        try { _thread?.Join(300); } catch { }
        Cleanup();
    }

    void Cleanup()
    {
        try
        {
            if (_port != null && _port.IsOpen) _port.Close();
            _port?.Dispose();
        }
        catch { }
        _port = null;
    }
}
#endif
