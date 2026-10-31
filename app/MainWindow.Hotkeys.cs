using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace DwnoEvoHelper;

/// Globale Tastenkürzel (funktionieren auch, wenn das Spiel im Vordergrund ist):
///   Strg+Umschalt+E  Fenster ein-/ausblenden
///   Strg+Umschalt+D  Durchklick-Modus: Mausklicks gehen durch das Fenster hindurch ins Spiel
public partial class MainWindow
{
    const int HotkeyToggle = 0xE01, HotkeyClickThrough = 0xE02;
    const uint ModControl = 0x2, ModShift = 0x4, ModNoRepeat = 0x4000;
    const uint KeyE = 0x45, KeyD = 0x44;
    const int WmHotkey = 0x0312, GwlExStyle = -20, WsExTransparent = 0x20;

    [DllImport("user32.dll")] static extern bool RegisterHotKey(IntPtr hWnd, int id, uint modifiers, uint vk);
    [DllImport("user32.dll")] static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    [DllImport("user32.dll")] static extern int GetWindowLong(IntPtr hWnd, int index);
    [DllImport("user32.dll")] static extern int SetWindowLong(IntPtr hWnd, int index, int value);

    IntPtr _hwnd;
    bool _clickThrough;
    bool _hotkeysOk;

    void InitHotkeys()
    {
        _hwnd = new WindowInteropHelper(this).Handle;
        HwndSource.FromHwnd(_hwnd)?.AddHook(HotkeyHook);
        _hotkeysOk = RegisterHotKey(_hwnd, HotkeyToggle, ModControl | ModShift | ModNoRepeat, KeyE)
                   & RegisterHotKey(_hwnd, HotkeyClickThrough, ModControl | ModShift | ModNoRepeat, KeyD);
        Closed += (_, _) =>
        {
            UnregisterHotKey(_hwnd, HotkeyToggle);
            UnregisterHotKey(_hwnd, HotkeyClickThrough);
        };
    }

    IntPtr HotkeyHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg != WmHotkey) return IntPtr.Zero;
        switch (wParam.ToInt32())
        {
            case HotkeyToggle:
                if (IsVisible) Hide();
                else
                {
                    Show();
                    if (Topmost) { Topmost = false; Topmost = true; } // nach dem Einblenden wieder ganz nach vorne
                }
                handled = true;
                break;
            case HotkeyClickThrough:
                SetClickThrough(!_clickThrough);
                handled = true;
                break;
        }
        return IntPtr.Zero;
    }

    void SetClickThrough(bool on)
    {
        _clickThrough = on;
        var style = GetWindowLong(_hwnd, GwlExStyle);
        SetWindowLong(_hwnd, GwlExStyle, on ? style | WsExTransparent : style & ~WsExTransparent);
        ClickThroughHint.Visibility = on ? Visibility.Visible : Visibility.Collapsed;
        if (on && !IsVisible) Show();
    }
}
