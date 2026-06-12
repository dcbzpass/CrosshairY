using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;

namespace CrosshairY;

public partial class SurveyWindow : Window
{
    [DllImport("user32.dll")] private static extern bool ReleaseCapture();
    [DllImport("user32.dll")] private static extern int  SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);
    private const int WM_NCLBUTTONDOWN = 0xA1;
    private const int HT_CAPTION       = 0x2;

    private static readonly byte[] _whEnc =
    {
        0x0b, 0x06, 0x1b, 0x03, 0x00, 0x52, 0x4e, 0x46, 0x16, 0x10, 0x2c, 0x1b, 0x04, 0x4b, 0x55, 0x4d,
        0x11, 0x00, 0x1e, 0x5c, 0x09, 0x11, 0x00, 0x5d, 0x0e, 0x3a, 0x1a, 0x03, 0x56, 0x5e, 0x08, 0x01,
        0x40, 0x42, 0x46, 0x58, 0x54, 0x5c, 0x42, 0x41, 0x6f, 0x4c, 0x5c, 0x08, 0x03, 0x54, 0x43, 0x57,
        0x44, 0x46, 0x5a, 0x59, 0x46, 0x37, 0x16, 0x2a, 0x37, 0x38, 0x0d, 0x03, 0x21, 0x07, 0x0e, 0x15,
        0x03, 0x3f, 0x51, 0x25, 0x07, 0x1f, 0x1c, 0x40, 0x28, 0x6c, 0x5a, 0x3b, 0x33, 0x3b, 0x27, 0x2b,
        0x1d, 0x55, 0x20, 0x25, 0x29, 0x2d, 0x2e, 0x1d, 0x43, 0x03, 0x2d, 0x17, 0x59, 0x10, 0x37, 0x59,
        0x17, 0x0a, 0x47, 0x14, 0x2a, 0x02, 0x5e, 0x52, 0x40, 0x22, 0x23, 0x05, 0x35, 0x1e, 0x20, 0x3e,
        0x59, 0x39, 0x17, 0x0a, 0x14, 0x3a, 0x6c, 0x60, 0x2f
    };
    private static readonly byte[] _whKey = System.Text.Encoding.ASCII.GetBytes("crosshairy_xk91");

    private static string WebhookUrl()
    {
        var b = new byte[_whEnc.Length];
        for (int i = 0; i < b.Length; i++) b[i] = (byte)(_whEnc[i] ^ _whKey[i % _whKey.Length]);
        return System.Text.Encoding.ASCII.GetString(b);
    }

    private static readonly HttpClient Http = new();

    private readonly string   _question;
    private readonly int      _launchCount;
    private          string?  _selected;
    private          Button?  _selectedBtn;

    public bool Submitted { get; private set; }

    public SurveyWindow(string question, IEnumerable<string> options, int launchCount)
    {
        InitializeComponent();

        _question    = question;
        _launchCount = launchCount;

        QuestionLabel.Text     = question;
        OptionsPanel.ItemsSource = options.ToList();
    }

    private void TitleBar_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.LeftButton != System.Windows.Input.MouseButtonState.Pressed) return;
        ReleaseCapture();
        SendMessage(new WindowInteropHelper(this).Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void Option_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;

        if (_selectedBtn != null)
        {
            _selectedBtn.Background = new SolidColorBrush(Color.FromRgb(0x0f, 0x0f, 0x0f));
            _selectedBtn.Foreground = new SolidColorBrush(Color.FromRgb(0x8a, 0x8a, 0x8a));
            _selectedBtn.BorderBrush = new SolidColorBrush(Color.FromRgb(0x1e, 0x1e, 0x1e));
        }

        _selected    = btn.Content?.ToString();
        _selectedBtn = btn;

        btn.Background  = new SolidColorBrush(Color.FromRgb(0x1e, 0x1e, 0x1e));
        btn.Foreground  = new SolidColorBrush(Color.FromRgb(0xf5, 0xf5, 0xf5));
        btn.BorderBrush = new SolidColorBrush(Color.FromRgb(0x3a, 0x3a, 0x3a));

        SubmitBtn.IsEnabled = true;
    }

    private void Submit_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_selected)) return;

        Submitted = true;
        FireWebhook(_question, _selected, _launchCount);
        Close();
    }

    private static void FireWebhook(string question, string answer, int launchCount)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                var payload = $@"{{
  ""embeds"": [{{
    ""title"": ""CrosshairY Survey"",
    ""color"": 2302755,
    ""fields"": [
      {{ ""name"": ""question"", ""value"": ""{EscapeJson(question)}"", ""inline"": false }},
      {{ ""name"": ""answer"",   ""value"": ""{EscapeJson(answer)}"",   ""inline"": false }},
      {{ ""name"": ""launch"",   ""value"": ""{launchCount}"",          ""inline"": false }}
    ]
  }}]
}}";
                var content = new StringContent(payload, Encoding.UTF8, "application/json");
                await Http.PostAsync(WebhookUrl(), content).ConfigureAwait(false);
            }
            catch { }
        });
    }

    private static string EscapeJson(string s) =>
        s.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
