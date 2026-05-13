using System.Runtime.InteropServices;
using System.Diagnostics;

namespace CrosshairY;

[StructLayout(LayoutKind.Sequential)]
internal struct KBDLLHOOKSTRUCT
{
    public uint   vkCode;
    public uint   scanCode;
    public uint   flags;
    public uint   time;
    public IntPtr dwExtraInfo;
}

[StructLayout(LayoutKind.Sequential)]
internal struct KEYBDINPUT
{
    public ushort wVk;
    public ushort wScan;
    public uint   dwFlags;
    public uint   time;
    public IntPtr dwExtraInfo;
}

[StructLayout(LayoutKind.Sequential)]
internal struct MOUSEINPUT
{
    public int    dx, dy;
    public uint   mouseData;
    public uint   dwFlags;
    public uint   time;
    public IntPtr dwExtraInfo;
}

[StructLayout(LayoutKind.Explicit)]
internal struct INPUT_UNION
{
    [FieldOffset(0)] public MOUSEINPUT  mi;
    [FieldOffset(0)] public KEYBDINPUT  ki;
}

[StructLayout(LayoutKind.Sequential)]
internal struct INPUT
{
    public uint        type;
    public INPUT_UNION u;
}

public sealed class GlobalKeyboardHook : IDisposable
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN     = 0x0100;
    private const int WM_KEYUP       = 0x0101;
    private const int WM_SYSKEYDOWN  = 0x0104;
    private const int WM_SYSKEYUP    = 0x0105;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

    private IntPtr   _kbHandle;
    private HookProc _kbProc;

    public event Action<string>? KeyDown;
    public event Action<string>? KeyUp;

    public GlobalKeyboardHook()
    {
        _kbProc = KbCallback;

        IntPtr hMod = GetModuleHandle(null);
        if (hMod == IntPtr.Zero)
        {
            using var cur = Process.GetCurrentProcess();
            var mod = cur.MainModule;
            if (mod != null) hMod = GetModuleHandle(mod.ModuleName);
        }

        _kbHandle = SetWindowsHookEx(WH_KEYBOARD_LL, _kbProc, hMod, 0);
        if (_kbHandle == IntPtr.Zero)
        {
            int err = Marshal.GetLastWin32Error();
            throw new InvalidOperationException(
                $"SetWindowsHookEx (keyboard) failed (Win32 error {err}). " +
                "Try running as administrator if error is 5.");
        }
    }

    private IntPtr KbCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var kb  = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
            var key = VkToString(kb.vkCode);
            if (wParam == WM_KEYDOWN   || wParam == WM_SYSKEYDOWN) KeyDown?.Invoke(key);
            if (wParam == WM_KEYUP     || wParam == WM_SYSKEYUP)   KeyUp?.Invoke(key);
        }
        return CallNextHookEx(_kbHandle, nCode, wParam, lParam);
    }

    public static string VkToString(uint vk) => vk switch
    {
        0x08 => "Key.backspace",
        0x09 => "Key.tab",
        0x0D => "Key.enter",
        0x10 => "Key.shift",
        0x11 => "Key.ctrl_l",
        0x12 => "Key.alt_l",
        0x14 => "Key.caps_lock",
        0x1B => "Key.esc",
        0x20 => "Key.space",
        0x21 => "Key.page_up",
        0x22 => "Key.page_down",
        0x23 => "Key.end",
        0x24 => "Key.home",
        0x25 => "Key.left",
        0x26 => "Key.up",
        0x27 => "Key.right",
        0x28 => "Key.down",
        0x2D => "Key.insert",
        0x2E => "Key.delete",
        0x70 => "Key.f1",  0x71 => "Key.f2",  0x72 => "Key.f3",
        0x73 => "Key.f4",  0x74 => "Key.f5",  0x75 => "Key.f6",
        0x76 => "Key.f7",  0x77 => "Key.f8",  0x78 => "Key.f9",
        0x79 => "Key.f10", 0x7A => "Key.f11", 0x7B => "Key.f12",
        0xA0 => "Key.shift",   0xA1 => "Key.shift_r",
        0xA2 => "Key.ctrl_l",  0xA3 => "Key.ctrl_r",
        0xA4 => "Key.alt_l",   0xA5 => "Key.alt_r",
        >= 0x30 and <= 0x39 => ((char)vk).ToString().ToLower(),
        >= 0x41 and <= 0x5A => ((char)vk).ToString().ToLower(),
        >= 0x60 and <= 0x69 => ((char)('0' + vk - 0x60)).ToString(),
        _ => $"vk{vk}"
    };

    public static ushort StringToVk(string key) => key switch
    {
        "Key.backspace" => 0x08,
        "Key.tab"       => 0x09,
        "Key.enter"     => 0x0D,
        "Key.shift"     => 0xA0,
        "Key.shift_r"   => 0xA1,
        "Key.ctrl_l"    => 0xA2,
        "Key.ctrl_r"    => 0xA3,
        "Key.alt_l"     => 0xA4,
        "Key.alt_r"     => 0xA5,
        "Key.caps_lock" => 0x14,
        "Key.esc"       => 0x1B,
        "Key.space"     => 0x20,
        "Key.page_up"   => 0x21,
        "Key.page_down" => 0x22,
        "Key.end"       => 0x23,
        "Key.home"      => 0x24,
        "Key.left"      => 0x25,
        "Key.up"        => 0x26,
        "Key.right"     => 0x27,
        "Key.down"      => 0x28,
        "Key.insert"    => 0x2D,
        "Key.delete"    => 0x2E,
        "Key.f1"  => 0x70, "Key.f2"  => 0x71, "Key.f3"  => 0x72,
        "Key.f4"  => 0x73, "Key.f5"  => 0x74, "Key.f6"  => 0x75,
        "Key.f7"  => 0x76, "Key.f8"  => 0x77, "Key.f9"  => 0x78,
        "Key.f10" => 0x79, "Key.f11" => 0x7A, "Key.f12" => 0x7B,
        { Length: 1 } s when char.IsLetter(s[0]) => (ushort)char.ToUpper(s[0]),
        { Length: 1 } s when char.IsDigit(s[0])  => (ushort)s[0],
        _ => 0
    };

    public void Dispose()
    {
        if (_kbHandle != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_kbHandle);
            _kbHandle = IntPtr.Zero;
        }
    }
}
