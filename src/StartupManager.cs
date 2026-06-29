using Microsoft.Win32;

namespace CrosshairY;

internal static class StartupManager
{
    private const string RunKey   = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "CrosshairY";

    private static string? ExePath()
    {
        try
        {
            return System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
        }
        catch
        {
            return null;
        }
    }

    public static bool IsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, false);
            return key?.GetValue(ValueName) is string;
        }
        catch
        {
            return false;
        }
    }

    public static void SetEnabled(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, true)
                            ?? Registry.CurrentUser.CreateSubKey(RunKey);
            if (key == null) return;

            if (enabled)
            {
                var path = ExePath();
                if (string.IsNullOrEmpty(path)) return;
                key.SetValue(ValueName, "\"" + path + "\"");
            }
            else
            {
                if (key.GetValue(ValueName) != null) key.DeleteValue(ValueName, false);
            }
        }
        catch { }
    }
}
