using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace CrosshairY;

public partial class App : System.Windows.Application
{
    private System.Windows.Forms.NotifyIcon?  _tray;
    private System.Windows.Forms.ContextMenuStrip? _trayMenu;

    private System.Windows.Forms.ToolStripMenuItem? _itemOverlay;
    private System.Windows.Forms.ToolStripMenuItem? _itemFollow;
    private System.Windows.Forms.ToolStripMenuItem? _itemProof;
    private System.Windows.Forms.ToolStripMenuItem? _itemProfiles;

    private System.Drawing.Font? _menuFont;
    private System.Drawing.Font? _menuFontBold;
    private TrayMenuRenderer? _menuRenderer;

    private static MainWindow? MainWin => Current.MainWindow as MainWindow;

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

        _menuFont     = new System.Drawing.Font("Courier New", 8.5f, System.Drawing.FontStyle.Regular);
        _menuFontBold = new System.Drawing.Font("Courier New", 8.5f, System.Drawing.FontStyle.Bold);
        _menuRenderer = new TrayMenuRenderer(bgDeep, bgBtn, bgHover, white, muted);

        var strip = new System.Windows.Forms.ContextMenuStrip
        {
            BackColor       = bgDeep,
            ForeColor       = white,
            ShowImageMargin = false,
            AutoSize        = true,
            MinimumSize     = new System.Drawing.Size(150, 0),
            RenderMode      = System.Windows.Forms.ToolStripRenderMode.Professional,
            Renderer        = _menuRenderer
        };

        var itemOpen = new System.Windows.Forms.ToolStripMenuItem("OPEN")
        {
            Font      = _menuFontBold,
            ForeColor = white
        };
        itemOpen.Click += (_, _) => ShowMainWindow();

        _itemOverlay = new System.Windows.Forms.ToolStripMenuItem("OVERLAY")
        {
            Font      = _menuFont,
            ForeColor = white
        };
        _itemOverlay.Click += (_, _) => Dispatcher.Invoke(() => MainWin?.TrayToggleOverlay());

        _itemFollow = new System.Windows.Forms.ToolStripMenuItem("FOLLOW CURSOR")
        {
            Font      = _menuFont,
            ForeColor = white
        };
        _itemFollow.Click += (_, _) => Dispatcher.Invoke(() => MainWin?.TrayToggleFollow());

        _itemProof = new System.Windows.Forms.ToolStripMenuItem("PROOF MODE")
        {
            Font      = _menuFont,
            ForeColor = white
        };
        _itemProof.Click += (_, _) => Dispatcher.Invoke(() => MainWin?.TrayToggleProof());

        _itemProfiles = new System.Windows.Forms.ToolStripMenuItem("PROFILES")
        {
            Font      = _menuFont,
            ForeColor = white
        };
        StyleDropDown(_itemProfiles.DropDown, bgDeep);

        var itemExit = new System.Windows.Forms.ToolStripMenuItem("EXIT")
        {
            Font      = _menuFontBold,
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
        strip.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        strip.Items.Add(_itemOverlay);
        strip.Items.Add(_itemFollow);
        strip.Items.Add(_itemProof);
        strip.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        strip.Items.Add(_itemProfiles);
        strip.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        strip.Items.Add(itemExit);

        strip.Opening += (_, _) => RefreshTrayMenu();

        return strip;
    }

    private void StyleDropDown(System.Windows.Forms.ToolStripDropDown dropDown, System.Drawing.Color bg)
    {
        dropDown.BackColor       = bg;
        dropDown.RenderMode      = System.Windows.Forms.ToolStripRenderMode.Professional;
        if (_menuRenderer != null) dropDown.Renderer = _menuRenderer;
        if (dropDown is System.Windows.Forms.ToolStripDropDownMenu menu)
            menu.ShowImageMargin = false;
    }

    private void RefreshTrayMenu()
    {
        var mw    = MainWin;
        var white = System.Drawing.ColorTranslator.FromHtml("#f5f5f5");
        var muted = System.Drawing.ColorTranslator.FromHtml("#5a5a5a");

        if (_itemOverlay != null)
            _itemOverlay.Text = "OVERLAY        " + (mw?.TrayOverlayOn == true ? "ON" : "OFF");
        if (_itemFollow != null)
            _itemFollow.Text = "FOLLOW CURSOR  " + (mw?.TrayFollowOn == true ? "ON" : "OFF");
        if (_itemProof != null)
            _itemProof.Text = "PROOF MODE     " + (mw?.TrayProofOn == true ? "ON" : "OFF");

        if (_itemProfiles == null) return;

        _itemProfiles.DropDownItems.Clear();
        var profiles = mw?.TrayProfiles();

        if (profiles == null || profiles.Count == 0)
        {
            var none = new System.Windows.Forms.ToolStripMenuItem("no configs saved")
            {
                Font      = _menuFont,
                ForeColor = muted,
                Enabled   = false
            };
            _itemProfiles.DropDownItems.Add(none);
            return;
        }

        foreach (var (name, path) in profiles)
        {
            var item = new System.Windows.Forms.ToolStripMenuItem(name)
            {
                Font      = _menuFont,
                ForeColor = white
            };
            string captured = path;
            item.Click += (_, _) => Dispatcher.Invoke(() => MainWin?.TrayLoadProfile(captured));
            _itemProfiles.DropDownItems.Add(item);
        }
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
