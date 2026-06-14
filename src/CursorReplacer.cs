using System.Runtime.InteropServices;

namespace CrosshairY;

internal static class CursorReplacer
{
    [StructLayout(LayoutKind.Sequential)]
    private struct ICONINFO
    {
        public bool   fIcon;
        public int    xHotspot;
        public int    yHotspot;
        public IntPtr hbmMask;
        public IntPtr hbmColor;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetSystemCursor(IntPtr hcur, uint id);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetIconInfo(IntPtr hIcon, out ICONINFO piconinfo);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr CreateIconIndirect(ref ICONINFO piconinfo);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr CopyIcon(IntPtr hIcon);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, IntPtr pvParam, uint fWinIni);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern bool DeleteObject(IntPtr hObject);

    private const uint SPI_SETCURSORS = 0x0057;
    private const uint SPIF_SENDCHANGE = 0x0002;

    private static readonly uint[] CursorIds =
    {
        32512, 32513, 32514, 32515, 32516,
        32642, 32643, 32644, 32645, 32646,
        32648, 32649, 32650, 32651
    };

    private static bool _active;

    public static void Apply(System.Drawing.Bitmap bmp, int hotX, int hotY)
    {
        IntPtr hCur = BuildCursor(bmp, hotX, hotY);
        if (hCur == IntPtr.Zero) return;

        foreach (var id in CursorIds)
        {
            IntPtr copy = CopyIcon(hCur);
            if (copy != IntPtr.Zero) SetSystemCursor(copy, id);
        }

        DestroyIcon(hCur);
        _active = true;
    }

    public static void Restore()
    {
        if (!_active) return;
        SystemParametersInfo(SPI_SETCURSORS, 0, IntPtr.Zero, SPIF_SENDCHANGE);
        _active = false;
    }

    public static void ForceRestore()
    {
        SystemParametersInfo(SPI_SETCURSORS, 0, IntPtr.Zero, SPIF_SENDCHANGE);
        _active = false;
    }

    private static IntPtr BuildCursor(System.Drawing.Bitmap bmp, int hotX, int hotY)
    {
        IntPtr tmpIcon = bmp.GetHicon();
        if (tmpIcon == IntPtr.Zero) return IntPtr.Zero;

        if (!GetIconInfo(tmpIcon, out var info))
        {
            DestroyIcon(tmpIcon);
            return IntPtr.Zero;
        }

        info.fIcon    = false;
        info.xHotspot = hotX;
        info.yHotspot = hotY;

        IntPtr hCur = CreateIconIndirect(ref info);

        if (info.hbmColor != IntPtr.Zero) DeleteObject(info.hbmColor);
        if (info.hbmMask  != IntPtr.Zero) DeleteObject(info.hbmMask);
        DestroyIcon(tmpIcon);

        return hCur;
    }
}
