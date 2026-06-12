using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;

namespace CrosshairY;

public partial class CrosshairOverlay : Window
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowDisplayAffinity(IntPtr hWnd, uint dwAffinity);
    private const uint WDA_NONE               = 0x00000000;
    private const uint WDA_EXCLUDEFROMCAPTURE = 0x00000011;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    private const int GWL_EXSTYLE       = -20;
    private const int WS_EX_LAYERED     = 0x00080000;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_TOOLWINDOW  = 0x00000080;
    private const int WS_EX_APPWINDOW   = 0x00040000;

    public CrosshairOverlay()
    {
        InitializeComponent();
        Width  = SystemParameters.PrimaryScreenWidth;
        Height = SystemParameters.PrimaryScreenHeight;
        Left   = 0;
        Top    = 0;

        SourceInitialized += (_, _) =>
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowDisplayAffinity(hwnd, WDA_NONE);
            int ex = GetWindowLong(hwnd, GWL_EXSTYLE);
            ex |=  WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW;
            ex &= ~WS_EX_APPWINDOW;
            SetWindowLong(hwnd, GWL_EXSTYLE, ex);
        };
    }

    public void SetProof(bool active)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        SetWindowDisplayAffinity(hwnd, active ? WDA_EXCLUDEFROMCAPTURE : WDA_NONE);
    }

    public void UpdateCrosshair(string template, string color, bool outline, int outlineSize, int size, int opacity, int gap)
    {
        if (string.IsNullOrEmpty(template))
        {
            if (IsVisible) Hide();
            return;
        }

        double cx = Width  / 2.0;
        double cy = Height / 2.0;
        OverlayCanvas.Opacity = opacity / 100.0;
        CrDraw.Draw(OverlayCanvas, cx, cy, size / 100.0, color, outline, outlineSize, template, gap);

        if (!IsVisible) Show();
    }

    // renders a custom pixel-grid crosshair from the list of "row,col,#hex" entries
    public void UpdateCustomCrosshair(List<string> pixels, int size, int opacity, int gridSize)
    {
        OverlayCanvas.Children.Clear();

        if (pixels.Count == 0)
        {
            if (IsVisible) Hide();
            return;
        }

        OverlayCanvas.Opacity = opacity / 100.0;

        double cx        = Width  / 2.0;
        double cy        = Height / 2.0;
        // keep the overall crosshair the same on-screen size regardless of grid
        // resolution: a finer grid just means smaller cells over the same area
        double baseField = 60.0; // px the whole grid spans at 100% size
        double cellSize  = baseField * (size / 100.0) / gridSize;
        // center the grid on the screen center using its real dimensions
        double gridOff   = (gridSize * cellSize) / 2.0;

        foreach (var entry in pixels)
        {
            var parts = entry.Split(',');
            if (parts.Length < 3) continue;
            if (!int.TryParse(parts[0], out int row) || !int.TryParse(parts[1], out int col)) continue;

            Color color;
            try   { color = (Color)ColorConverter.ConvertFromString(parts[2]); }
            catch { color = Colors.White; }

            var rect = new Rectangle
            {
                Width      = cellSize,
                Height     = cellSize,
                Fill       = new SolidColorBrush(color),
                SnapsToDevicePixels = true
            };

            Canvas.SetLeft(rect, cx - gridOff + col * cellSize);
            Canvas.SetTop(rect,  cy - gridOff + row * cellSize);
            OverlayCanvas.Children.Add(rect);
        }

        if (!IsVisible) Show();
    }
}

internal static class CrDraw
{
    public static void Draw(Canvas canvas, double cx, double cy, double scale,
        string colorHex, bool outline, double outSize, string template, int gap = 3)
    {
        canvas.Children.Clear();

        Color col;
        try   { col = (Color)ColorConverter.ConvertFromString(colorHex); }
        catch { col = Colors.White; }

        var brush = new SolidColorBrush(col);
        var black = Brushes.Black;

        switch (template)
        {
            case "dot":
                if (outline) PutEllipseFilled(canvas, cx, cy, (3 + outSize) * scale, black);
                PutEllipseFilled(canvas, cx, cy, 3 * scale, brush);
                break;

            case "ring":
            {
                double r  = 5  * scale;
                double st = 1.5 * scale;
                if (outline) PutEllipseStroked(canvas, cx, cy, r, st + outSize * 2, black);
                PutEllipseStroked(canvas, cx, cy, r, st, brush);
                break;
            }

            case "sq_dot":
                if (outline) PutRect(canvas, cx, cy, (3 + outSize) * scale, (3 + outSize) * scale, black);
                PutRect(canvas, cx, cy, 3 * scale, 3 * scale, brush);
                break;

            case "thin_cross":
                DrawCrossLines(canvas, cx, cy, scale, brush, black, outline, outSize, gap, 8, 1.5, false, false, false);
                break;

            case "thick_cross":
                DrawCrossLines(canvas, cx, cy, scale, brush, black, outline, outSize, gap, 6, 3.0, false, false, false);
                break;

            case "cross_dot_c":
                DrawCrossLines(canvas, cx, cy, scale, brush, black, outline, outSize, gap, 8, 1.5, true, false, false);
                break;

            case "t_shape":
                DrawCrossLines(canvas, cx, cy, scale, brush, black, outline, outSize, gap, 8, 1.5, false, true, false);
                break;

            case "cross_circle":
                DrawCrossLines(canvas, cx, cy, scale, brush, black, outline, outSize, gap, 8, 1.5, false, false, true);
                break;

            case "small_plus":
                DrawCrossLines(canvas, cx, cy, scale, brush, black, outline, outSize, 0, 5, 1.5, false, false, false);
                break;

            case "large_plus":
                DrawCrossLines(canvas, cx, cy, scale, brush, black, outline, outSize, 0, 12, 1.5, false, false, false);
                break;

            case "sniper":
                DrawCrossLines(canvas, cx, cy, scale, brush, black, outline, outSize, gap * 5, 40, 1.0, false, false, false);
                break;

            case "x_cross":
                DrawXLines(canvas, cx, cy, scale, brush, black, outline, outSize, 8, 1.5, false);
                break;

            case "x_dot":
                DrawXLines(canvas, cx, cy, scale, brush, black, outline, outSize, 8, 1.5, true);
                break;

            case "inward_arrows":
                DrawInwardArrows(canvas, cx, cy, scale, brush, black, outline, outSize);
                break;

            case "outward_chevrons":
                DrawOutwardChevrons(canvas, cx, cy, scale, brush, black, outline, outSize);
                break;

            case "triangle":
                DrawTriangle(canvas, cx, cy, scale, brush, black, outline, outSize);
                break;

            case "diamond":
                DrawDiamond(canvas, cx, cy, scale, brush, black, outline, outSize);
                break;
        }
    }

    static void DrawCrossLines(Canvas c, double cx, double cy, double s,
        Brush brush, Brush black, bool outline, double outSize,
        double gap, double len, double thick,
        bool withDot, bool noTop, bool withCircle)
    {
        double g = gap  * s;
        double l = len  * s;
        double t = thick * s;

        var arms = new List<(double x1, double y1, double x2, double y2)>();
        if (!noTop) arms.Add((cx, cy - g, cx, cy - g - l));
        arms.Add((cx, cy + g, cx, cy + g + l));
        arms.Add((cx - g, cy, cx - g - l, cy));
        arms.Add((cx + g, cy, cx + g + l, cy));

        if (outline)
            foreach (var (x1, y1, x2, y2) in arms)
                PutLine(c, x1, y1, x2, y2, black, t + outSize * 2);
        foreach (var (x1, y1, x2, y2) in arms)
            PutLine(c, x1, y1, x2, y2, brush, t);

        if (withDot)
        {
            double dr = 2.5 * s;
            if (outline) PutEllipseFilled(c, cx, cy, dr + outSize, black);
            PutEllipseFilled(c, cx, cy, dr, brush);
        }

        if (withCircle)
        {
            double cr = 12 * s;
            double cs = 1.5 * s;
            if (outline) PutEllipseStroked(c, cx, cy, cr, cs + outSize * 2, black);
            PutEllipseStroked(c, cx, cy, cr, cs, brush);
        }
    }

    static void DrawXLines(Canvas c, double cx, double cy, double s,
        Brush brush, Brush black, bool outline, double outSize,
        double len, double thick, bool withDot)
    {
        double l = len   * s;
        double t = thick * s;

        if (outline)
        {
            PutLine(c, cx - l, cy - l, cx + l, cy + l, black, t + outSize * 2);
            PutLine(c, cx + l, cy - l, cx - l, cy + l, black, t + outSize * 2);
        }
        PutLine(c, cx - l, cy - l, cx + l, cy + l, brush, t);
        PutLine(c, cx + l, cy - l, cx - l, cy + l, brush, t);

        if (withDot)
        {
            double dr = 2.5 * s;
            if (outline) PutEllipseFilled(c, cx, cy, dr + outSize, black);
            PutEllipseFilled(c, cx, cy, dr, brush);
        }
    }

    static void DrawInwardArrows(Canvas c, double cx, double cy, double s,
        Brush brush, Brush black, bool outline, double outSize)
    {
        double gap = 8 * s;
        double aw  = 8 * s;
        double ah  = 7 * s;

        var groups = new Point[][]
        {
            new[] { new Point(cx, cy - gap),      new Point(cx - aw / 2, cy - gap - ah), new Point(cx + aw / 2, cy - gap - ah) },
            new[] { new Point(cx, cy + gap),      new Point(cx - aw / 2, cy + gap + ah), new Point(cx + aw / 2, cy + gap + ah) },
            new[] { new Point(cx - gap, cy),      new Point(cx - gap - ah, cy - aw / 2), new Point(cx - gap - ah, cy + aw / 2) },
            new[] { new Point(cx + gap, cy),      new Point(cx + gap + ah, cy - aw / 2), new Point(cx + gap + ah, cy + aw / 2) }
        };

        foreach (var pts in groups)
        {
            if (outline)
            {
                var pOut = new Polygon { Stroke = black, StrokeThickness = outSize * 2, Fill = black };
                foreach (var p in pts) pOut.Points.Add(p);
                c.Children.Add(pOut);
            }
            var poly = new Polygon { Fill = brush, StrokeThickness = 0 };
            foreach (var p in pts) poly.Points.Add(p);
            c.Children.Add(poly);
        }
    }

    static void DrawOutwardChevrons(Canvas c, double cx, double cy, double s,
        Brush brush, Brush black, bool outline, double outSize)
    {
        double gap   = 5 * s;
        double chevW = 6 * s;
        double chevH = 5 * s;
        double t     = 1.5 * s;

        var chevrons = new (Point apex, Point p1, Point p2)[]
        {
            (new Point(cx,             cy - gap - chevH), new Point(cx - chevW, cy - gap),        new Point(cx + chevW, cy - gap)),
            (new Point(cx,             cy + gap + chevH), new Point(cx - chevW, cy + gap),        new Point(cx + chevW, cy + gap)),
            (new Point(cx - gap - chevH, cy),             new Point(cx - gap,   cy - chevW),      new Point(cx - gap,   cy + chevW)),
            (new Point(cx + gap + chevH, cy),             new Point(cx + gap,   cy - chevW),      new Point(cx + gap,   cy + chevW))
        };

        foreach (var (apex, p1, p2) in chevrons)
        {
            if (outline)
            {
                PutLine(c, apex.X, apex.Y, p1.X, p1.Y, black, t + outSize * 2);
                PutLine(c, apex.X, apex.Y, p2.X, p2.Y, black, t + outSize * 2);
            }
            PutLine(c, apex.X, apex.Y, p1.X, p1.Y, brush, t);
            PutLine(c, apex.X, apex.Y, p2.X, p2.Y, brush, t);
        }
    }

    static void DrawTriangle(Canvas c, double cx, double cy, double s,
        Brush brush, Brush black, bool outline, double outSize)
    {
        double h  = 18 * s;
        double w  = 16 * s;
        double st = Thick(1.5 * s);

        var pts = new[]
        {
            new Point(cx,         cy - 2 * h / 3),
            new Point(cx - w / 2, cy + h / 3),
            new Point(cx + w / 2, cy + h / 3)
        };

        if (outline)
        {
            var pOut = new Polygon { Stroke = black, StrokeThickness = Thick(st + outSize * 2), Fill = Brushes.Transparent };
            foreach (var p in pts) pOut.Points.Add(p);
            c.Children.Add(pOut);
        }
        var poly = new Polygon { Stroke = brush, StrokeThickness = st, Fill = Brushes.Transparent };
        foreach (var p in pts) poly.Points.Add(p);
        c.Children.Add(poly);
    }

    static void DrawDiamond(Canvas c, double cx, double cy, double s,
        Brush brush, Brush black, bool outline, double outSize)
    {
        double r  = System.Math.Max(1.0, 10 * s);
        double st = Thick(1.5 * s);

        var pts = new[]
        {
            new Point(cx,     cy - r),
            new Point(cx + r, cy),
            new Point(cx,     cy + r),
            new Point(cx - r, cy)
        };

        if (outline)
        {
            var pOut = new Polygon { Stroke = black, StrokeThickness = Thick(st + outSize * 2), Fill = Brushes.Transparent };
            foreach (var p in pts) pOut.Points.Add(p);
            c.Children.Add(pOut);
        }
        var poly = new Polygon { Stroke = brush, StrokeThickness = st, Fill = Brushes.Transparent };
        foreach (var p in pts) poly.Points.Add(p);
        c.Children.Add(poly);
    }

    static double Thick(double t) => System.Math.Max(0.5, t);

    static void PutLine(Canvas c, double x1, double y1, double x2, double y2, Brush stroke, double thick)
    {
        c.Children.Add(new Line
        {
            X1 = x1, Y1 = y1, X2 = x2, Y2 = y2,
            Stroke              = stroke,
            StrokeThickness     = Thick(thick),
            StrokeStartLineCap  = PenLineCap.Square,
            StrokeEndLineCap    = PenLineCap.Square
        });
    }

    static void PutEllipseFilled(Canvas c, double cx, double cy, double r, Brush fill)
    {
        r = System.Math.Max(0.5, r);
        var e = new Ellipse { Width = r * 2, Height = r * 2, Fill = fill };
        Canvas.SetLeft(e, cx - r);
        Canvas.SetTop(e,  cy - r);
        c.Children.Add(e);
    }

    static void PutEllipseStroked(Canvas c, double cx, double cy, double r, double thick, Brush stroke)
    {
        r = System.Math.Max(1.0, r);
        var e = new Ellipse
        {
            Width           = r * 2,
            Height          = r * 2,
            Stroke          = stroke,
            StrokeThickness = Thick(System.Math.Min(thick, r)),
            Fill            = Brushes.Transparent
        };
        Canvas.SetLeft(e, cx - r);
        Canvas.SetTop(e,  cy - r);
        c.Children.Add(e);
    }

    static void PutRect(Canvas c, double cx, double cy, double hw, double hh, Brush fill)
    {
        hw = System.Math.Max(0.5, hw);
        hh = System.Math.Max(0.5, hh);
        var r = new Rectangle { Width = hw * 2, Height = hh * 2, Fill = fill };
        Canvas.SetLeft(r, cx - hw);
        Canvas.SetTop(r,  cy - hh);
        c.Children.Add(r);
    }
}