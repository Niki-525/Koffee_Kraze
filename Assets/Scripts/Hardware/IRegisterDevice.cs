// Scripts/Hardware/IRegisterDevice.cs
using System;

/// <summary>
/// Anything that can act as the cash register's input/output: a real
/// microcontroller over USB serial, the keyboard, etc.
///
/// Keys are reported as characters: '0'-'9' for digits, 'C' for clear,
/// 'E' for enter.
/// </summary>
public interface IRegisterDevice : IDisposable
{
    string Name { get; }
    bool IsConnected { get; }

    event Action<char> KeyPressed;
    event Action DrawerButtonPressed;

    /// <summary>Called once per frame on the main thread. Pump input here.</summary>
    void Tick();

    /// <summary>Show two lines of text on the device display (if it has one).</summary>
    void ShowLines(string top, string bottom);

    /// <summary>Pop the cash drawer (if it has one).</summary>
    void OpenDrawer();
}
