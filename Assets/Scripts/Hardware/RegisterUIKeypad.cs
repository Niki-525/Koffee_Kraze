// Scripts/Hardware/RegisterUIKeypad.cs
using UnityEngine;

/// <summary>
/// On-screen keypad for mouse/touch play or when no hardware is attached.
/// Put this on a panel, then for each Button add an OnClick entry that calls
/// Press with a string argument: "0".."9", "C" (clear), "E" (enter).
/// The drawer button can call PressDrawer.
/// </summary>
public class RegisterUIKeypad : MonoBehaviour
{
    public void Press(string key)
    {
        if (string.IsNullOrEmpty(key)) return;
        RegisterManager.Instance?.InjectKey(char.ToUpperInvariant(key[0]));
    }

    public void PressDrawer() => RegisterManager.Instance?.InjectDrawerPress();
}
