// Scripts/Hardware/KeyboardRegisterDevice.cs
using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Stand-in register that uses the computer keyboard, so the game is fully
/// playable and testable with no hardware.
///
///   0-9 (row or numpad) = digits
///   Enter               = submit
///   Backspace / Delete  = clear
///   Space               = drawer button
///
/// Works with either the new Input System or the legacy Input Manager,
/// whichever your project has enabled.
/// </summary>
public class KeyboardRegisterDevice : IRegisterDevice
{
    public string Name => "Keyboard";
    public bool IsConnected => true;

    public event Action<char> KeyPressed;
    public event Action DrawerButtonPressed;

#if ENABLE_INPUT_SYSTEM
    static readonly Key[] DigitKeys =
    {
        Key.Digit0, Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4,
        Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9
    };
    static readonly Key[] NumpadKeys =
    {
        Key.Numpad0, Key.Numpad1, Key.Numpad2, Key.Numpad3, Key.Numpad4,
        Key.Numpad5, Key.Numpad6, Key.Numpad7, Key.Numpad8, Key.Numpad9
    };

    public void Tick()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        for (int i = 0; i < 10; i++)
        {
            if (kb[DigitKeys[i]].wasPressedThisFrame || kb[NumpadKeys[i]].wasPressedThisFrame)
                KeyPressed?.Invoke((char)('0' + i));
        }

        if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
            KeyPressed?.Invoke('E');
        if (kb.backspaceKey.wasPressedThisFrame || kb.deleteKey.wasPressedThisFrame)
            KeyPressed?.Invoke('C');
        if (kb.spaceKey.wasPressedThisFrame)
            DrawerButtonPressed?.Invoke();
    }
#elif ENABLE_LEGACY_INPUT_MANAGER
    public void Tick()
    {
        for (int i = 0; i < 10; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha0 + i) || Input.GetKeyDown(KeyCode.Keypad0 + i))
                KeyPressed?.Invoke((char)('0' + i));
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            KeyPressed?.Invoke('E');
        if (Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.Delete))
            KeyPressed?.Invoke('C');
        if (Input.GetKeyDown(KeyCode.Space))
            DrawerButtonPressed?.Invoke();
    }
#else
    public void Tick() { }
#endif

    // The on-screen UI shows everything the keyboard can't, so no-ops here.
    public void ShowLines(string top, string bottom) { }

    public void OpenDrawer() => Debug.Log("[Register] Drawer opens (virtual).");

    public void Dispose() { }
}
