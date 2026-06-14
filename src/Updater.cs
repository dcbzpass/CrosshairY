using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace CrosshairY;

internal static class Updater
{
    private const string Owner = "dcbzpass";
    private const string Repo  = "CrosshairY";

    public sealed record Release(string Tag, string ExeUrl, long ExeSize, string HtmlUrl);

    private static HttpClient CreateClient()
    {
        var http = new HttpClient();
        http.DefaultRequestHeaders.UserAgent.ParseAdd("CrosshairY-Updater");
        return http;
    }

    public static async Task<Release?> GetLatestAsync()
    {
        try
        {
            using var http = CreateClient();
            http.Timeout = TimeSpan.FromSeconds(10);

            var json = await http.GetStringAsync($"https://api.github.com/repos/{Owner}/{Repo}/releases/latest");
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("tag_name", out var tagEl)) return null;
            var tag  = tagEl.GetString() ?? "";
            var html = root.TryGetProperty("html_url", out var h) ? h.GetString() ?? "" : "";

            var  exeUrl  = "";
            long exeSize = 0;
            if (root.TryGetProperty("assets", out var assets) && assets.ValueKind == JsonValueKind.Array)
            {
                foreach (var a in assets.EnumerateArray())
                {
                    var name = a.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                    if (!name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) continue;

                    if (a.TryGetProperty("browser_download_url", out var u))
                        exeUrl = u.GetString() ?? "";
                    exeSize = a.TryGetProperty("size", out var sz) && sz.TryGetInt64(out var s) ? s : 0;

                    if (string.Equals(name, "CrosshairY.exe", StringComparison.OrdinalIgnoreCase)) break;
                }
            }

            return new Release(tag, exeUrl, exeSize, html);
        }
        catch
        {
            return null;
        }
    }

    public static bool IsNewer(string current, string tag)
    {
        var t = tag.TrimStart('v', 'V');
        return Version.TryParse(current, out var cv)
            && Version.TryParse(t, out var tv)
            && tv > cv;
    }

    public static async Task<string?> DownloadAsync(string url, long expectedSize)
    {
        try
        {
            var dir = Path.Combine(Path.GetTempPath(), "CrosshairY_update");
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, "CrosshairY.exe");

            using var http = CreateClient();
            http.Timeout = TimeSpan.FromMinutes(5);

            var bytes = await http.GetByteArrayAsync(url);
            if (bytes.Length == 0) return null;
            if (expectedSize > 0 && bytes.Length != expectedSize) return null;

            await File.WriteAllBytesAsync(path, bytes);
            return path;
        }
        catch
        {
            return null;
        }
    }

    public static bool LaunchSwapAndExit(string newExe)
    {
        try
        {
            var target = Environment.ProcessPath;
            if (string.IsNullOrEmpty(target)) return false;

            var pid = Environment.ProcessId;
            var dir = Path.Combine(Path.GetTempPath(), "CrosshairY_update");
            Directory.CreateDirectory(dir);
            var bat = Path.Combine(dir, "update.bat");

            var script =
                "@echo off\r\n" +
                ":loop\r\n" +
                $"tasklist /fi \"PID eq {pid}\" | find \"{pid}\" >nul\r\n" +
                "if %errorlevel%==0 (\r\n" +
                "  timeout /t 1 /nobreak >nul\r\n" +
                "  goto loop\r\n" +
                ")\r\n" +
                $"copy /y \"{newExe}\" \"{target}\" >nul\r\n" +
                $"start \"\" \"{target}\"\r\n" +
                "del \"%~f0\"\r\n";

            File.WriteAllText(bat, script);

            var psi = new ProcessStartInfo
            {
                FileName         = "cmd.exe",
                Arguments        = $"/c \"\"{bat}\"\"",
                CreateNoWindow   = true,
                UseShellExecute  = false,
                WindowStyle      = ProcessWindowStyle.Hidden,
                WorkingDirectory = dir
            };

            Process.Start(psi);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
