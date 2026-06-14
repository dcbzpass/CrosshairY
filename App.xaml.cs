using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace CrosshairY;

public partial class App : System.Windows.Application
{
    private System.Windows.Forms.NotifyIcon?  _tray;
    private System.Windows.Forms.ContextMenuStrip? _trayMenu;

    protected override void OnStartup(StartupEventArgs e)
    {
        CursorReplacer.ForceRestore();

        DispatcherUnhandledException += (_, args) =>
        {
            CursorReplacer.Restore();
            System.Windows.MessageBox.Show(
                $"Unhandled exception:\n\n{args.Exception}",
                "CrosshairY - Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            args.Handled = true;
            Shutdown(1);
        };

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            CursorReplacer.Restore();
            System.Windows.MessageBox.Show(
                $"Fatal exception:\n\n{args.ExceptionObject}",
                "CrosshairY - Fatal Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        };

        ProcessExit_Hook();

        base.OnStartup(e);

        BuildTrayIcon();
    }

    private void BuildTrayIcon()
    {
        System.Drawing.Icon? icon = null;
        try
        {
            var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
            if (exePath != null && File.Exists(exePath))
                icon = System.Drawing.Icon.ExtractAssociatedIcon(exePath);
        }
        catch { }
        icon ??= System.Drawing.SystemIcons.Application;

        _trayMenu = BuildTrayMenu();

        _tray = new System.Windows.Forms.NotifyIcon
        {
            Icon             = icon,
            Text             = "CrosshairY",
            Visible          = true,
            ContextMenuStrip = _trayMenu
        };

        _tray.DoubleClick += (_, _) => ShowMainWindow();
    }

    internal void RebuildTrayMenu()
    {
        if (_tray == null) return;
        _trayMenu?.Dispose();
        _trayMenu            = BuildTrayMenu();
        _tray.ContextMenuStrip = _trayMenu;
    }

    private System.Windows.Forms.ContextMenuStrip BuildTrayMenu()
    {
        var bgDeep   = System.Drawing.ColorTranslator.FromHtml("#080808");
        var bgBtn    = System.Drawing.ColorTranslator.FromHtml("#141414");
        var bgHover  = System.Drawing.ColorTranslator.FromHtml("#1e1e1e");
        var white    = System.Drawing.ColorTranslator.FromHtml("#f5f5f5");
        var muted    = System.Drawing.ColorTranslator.FromHtml("#5a5a5a");
        var red      = System.Drawing.ColorTranslator.FromHtml("#aa2020");
        var redHover = System.Drawing.ColorTranslator.FromHtml("#cc2828");

        var font     = new System.Drawing.Font("Courier New", 8.5f, System.Drawing.FontStyle.Regular);
        var fontBold = new System.Drawing.Font("Courier New", 8.5f, System.Drawing.FontStyle.Bold);

        var strip = new System.Windows.Forms.ContextMenuStrip
        {
            BackColor       = bgDeep,
            ForeColor       = white,
            ShowImageMargin = false,
            AutoSize        = true,
            MinimumSize     = new System.Drawing.Size(90, 0),
            RenderMode      = System.Windows.Forms.ToolStripRenderMode.Professional,
            Renderer        = new TrayMenuRenderer(bgDeep, bgBtn, bgHover, white, muted)
        };

        var itemOpen = new System.Windows.Forms.ToolStripMenuItem("OPEN")
        {
            Font      = fontBold,
            ForeColor = white
        };
        itemOpen.Click += (_, _) => ShowMainWindow();

        var sep = new System.Windows.Forms.ToolStripSeparator();

        var itemExit = new System.Windows.Forms.ToolStripMenuItem("EXIT")
        {
            Font      = fontBold,
            ForeColor = red
        };
        itemExit.MouseEnter += (_, _) => itemExit.ForeColor = redHover;
        itemExit.MouseLeave += (_, _) => itemExit.ForeColor = red;
        itemExit.Click      += (_, _) =>
        {
            _tray!.Visible = false;
            _tray.Dispose();
            Dispatcher.Invoke(() =>
            {
                if (Current.MainWindow is MainWindow mw) mw.PrepareForExit();
                Shutdown(0);
            });
        };

        strip.Items.Add(itemOpen);
        strip.Items.Add(sep);
        strip.Items.Add(itemExit);

        return strip;
    }

    private static void ShowMainWindow()
    {
        var win = Current.MainWindow;
        if (win == null) return;
        if (win.WindowState == WindowState.Minimized)
            win.WindowState = WindowState.Normal;
        win.Show();
        win.Activate();
    }

    private static void ProcessExit_Hook()
    {
        AppDomain.CurrentDomain.ProcessExit += (_, _) => CursorReplacer.Restore();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        CursorReplacer.Restore();
        if (_tray != null)
        {
            _tray.Visible = false;
            _tray.Dispose();
        }
        _trayMenu?.Dispose();
        base.OnExit(e);
    }
}

internal class TrayMenuRenderer : System.Windows.Forms.ToolStripProfessionalRenderer
{
    private readonly System.Drawing.Color _bg;
    private readonly System.Drawing.Color _bgBtn;
    private readonly System.Drawing.Color _bgHover;
    private readonly System.Drawing.Color _fg;
    private readonly System.Drawing.Color _muted;

    public TrayMenuRenderer(
        System.Drawing.Color bg,
        System.Drawing.Color bgBtn,
        System.Drawing.Color bgHover,
        System.Drawing.Color fg,
        System.Drawing.Color muted)
        : base(new TrayMenuColorTable(bg, bgBtn, bgHover))
    {
        _bg      = bg;
        _bgBtn   = bgBtn;
        _bgHover = bgHover;
        _fg      = fg;
        _muted   = muted;
    }

    protected override void OnRenderMenuItemBackground(System.Windows.Forms.ToolStripItemRenderEventArgs e)
    {
        var g    = e.Graphics;
        var item = e.Item;
        var rect = new System.Drawing.Rectangle(System.Drawing.Point.Empty, item.Size);

        if (item is System.Windows.Forms.ToolStripSeparator)
        {
            g.Clear(_bg);
            using var pen = new System.Drawing.Pen(System.Drawing.ColorTranslator.FromHtml("#1e1e1e"));
            int mid = rect.Height / 2;
            g.DrawLine(pen, 6, mid, rect.Width - 6, mid);
            return;
        }

        g.Clear(item.Selected ? _bgHover : _bg);
    }

    protected override void OnRenderToolStripBackground(System.Windows.Forms.ToolStripRenderEventArgs e)
    {
        e.Graphics.Clear(_bg);
    }

    protected override void OnRenderSeparator(System.Windows.Forms.ToolStripSeparatorRenderEventArgs e)
    {
        var g    = e.Graphics;
        var rect = new System.Drawing.Rectangle(System.Drawing.Point.Empty, e.Item.Size);
        g.Clear(_bg);
        using var pen = new System.Drawing.Pen(System.Drawing.ColorTranslator.FromHtml("#1e1e1e"));
        int mid = rect.Height / 2;
        g.DrawLine(pen, 6, mid, rect.Width - 6, mid);
    }

    protected override void OnRenderItemText(System.Windows.Forms.ToolStripItemTextRenderEventArgs e)
    {
        e.TextColor = e.Item.ForeColor;
        base.OnRenderItemText(e);
    }

    protected override void OnRenderToolStripBorder(System.Windows.Forms.ToolStripRenderEventArgs e)
    {
        using var pen = new System.Drawing.Pen(System.Drawing.ColorTranslator.FromHtml("#1e1e1e"));
        var rect = new System.Drawing.Rectangle(0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
        e.Graphics.DrawRectangle(pen, rect);
    }
}

internal class TrayMenuColorTable : System.Windows.Forms.ProfessionalColorTable
{
    private readonly System.Drawing.Color _bg;
    private readonly System.Drawing.Color _bgBtn;
    private readonly System.Drawing.Color _bgHover;

    public TrayMenuColorTable(
        System.Drawing.Color bg,
        System.Drawing.Color bgBtn,
        System.Drawing.Color bgHover)
    {
        _bg      = bg;
        _bgBtn   = bgBtn;
        _bgHover = bgHover;
    }

    public override System.Drawing.Color MenuItemSelected        => _bgHover;
    public override System.Drawing.Color MenuItemBorder          => _bgHover;
    public override System.Drawing.Color MenuBorder              => System.Drawing.ColorTranslator.FromHtml("#1e1e1e");
    public override System.Drawing.Color ToolStripDropDownBackground => _bg;
    public override System.Drawing.Color ImageMarginGradientBegin => _bg;
    public override System.Drawing.Color ImageMarginGradientMiddle => _bg;
    public override System.Drawing.Color ImageMarginGradientEnd   => _bg;
    public override System.Drawing.Color MenuItemSelectedGradientBegin => _bgHover;
    public override System.Drawing.Color MenuItemSelectedGradientEnd   => _bgHover;
    public override System.Drawing.Color MenuItemPressedGradientBegin  => _bgBtn;
    public override System.Drawing.Color MenuItemPressedGradientEnd    => _bgBtn;
    public override System.Drawing.Color SeparatorDark  => System.Drawing.ColorTranslator.FromHtml("#1e1e1e");
    public override System.Drawing.Color SeparatorLight => System.Drawing.ColorTranslator.FromHtml("#1e1e1e");
}
