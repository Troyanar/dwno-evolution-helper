using System;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DwnoEvoHelper;

/// Prüft beim Start, ob auf GitHub eine neuere Version veröffentlicht ist.
public static class Updates
{
    public const string Repo = "Troyanar/dwno-evo-helfer";
    public static string ReleasesPage => $"https://github.com/{Repo}/releases/latest";

    public static Version Current => Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0);

    /// Neuere Version (z. B. 1.6.0) oder null
    public static async Task<Version?> CheckAsync()
    {
        if (Repo.StartsWith("OWNER/")) return null;
        try
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
            http.DefaultRequestHeaders.UserAgent.ParseAdd("DWNO-Evo-Helfer/" + Current);
            var json = await http.GetStringAsync($"https://api.github.com/repos/{Repo}/releases/latest");
            var m = Regex.Match(json, "\"tag_name\"\\s*:\\s*\"v?([0-9.]+)\"");
            if (!m.Success || !Version.TryParse(m.Groups[1].Value, out var latest)) return null;
            return Normalize(latest) > Normalize(Current) ? latest : null;
        }
        catch
        {
            return null; // offline o. ä. – kein Hinweis
        }
    }

    static Version Normalize(Version v) => new(v.Major, v.Minor, Math.Max(v.Build, 0));
}
