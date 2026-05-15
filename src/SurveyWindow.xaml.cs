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

    private const string WebhookUrl = "https://discord.com/api/webhooks/1504772887696113804/pLskyv13LUvjFY7JSsYp2CIK1jp9t6sa3uRJ_rn95yOJoEaWLau0s6nCCnva2koy4mmE";

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
                await Http.PostAsync(WebhookUrl, content).ConfigureAwait(false);
            }
            catch { }
        });
    }

    private static string EscapeJson(string s) =>
        s.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
