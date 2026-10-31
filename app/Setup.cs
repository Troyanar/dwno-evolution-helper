using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace DwnoEvoHelper;

public enum PartState { Missing, Installed, Outdated }

/// Finds the game and installs BepInEx + the embedded plugin.
public static class Setup
{
    const string SteamAppId = "1530160";
    const string GameExe = "Digimon World Next Order.exe";
    const string PluginFile = "DWNO_EvolutionHelper.dll";
    const string LegacyPluginFile = "DWNO_EvoHelfer.dll"; // name up to 1.5

    // Tested BepInEx version (official build server)
    const string BepInExUrl =
        "https://builds.bepinex.dev/projects/bepinex_be/791/BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.791%2Bb782da4.zip";

    public static string? FindGameDir(string? remembered)
    {
        if (IsGameDir(remembered)) return remembered;
        foreach (var lib in SteamLibraries())
        {
            var manifest = Path.Combine(lib, "steamapps", $"appmanifest_{SteamAppId}.acf");
            var installDir = "Digimon World Next Order";
            if (File.Exists(manifest))
            {
                var m = Regex.Match(File.ReadAllText(manifest), "\"installdir\"\\s+\"([^\"]+)\"");
                if (m.Success) installDir = m.Groups[1].Value;
            }
            var dir = Path.Combine(lib, "steamapps", "common", installDir);
            if (IsGameDir(dir)) return dir;
        }

        // typical library folders on every fixed drive (Steam not registered, moved libraries, ...)
        foreach (var drive in FixedDrives())
        foreach (var lib in new[] { "SteamLibrary", "Steam", @"Program Files (x86)\Steam", @"Program Files\Steam",
                                    @"Games\Steam", @"Games\SteamLibrary", @"Spiele\Steam", @"Spiele\SteamLibrary", "Games", "Spiele" })
        {
            foreach (var dir in new[]
                     {
                         Path.Combine(drive, lib, "steamapps", "common", "Digimon World Next Order"),
                         Path.Combine(drive, lib, "Digimon World Next Order"),
                     })
                if (IsGameDir(dir)) return dir;
        }
        return null;
    }

    static IEnumerable<string> FixedDrives() =>
        DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.Fixed && d.IsReady).Select(d => d.RootDirectory.FullName);

    public static bool IsGameDir(string? dir) => dir != null && File.Exists(Path.Combine(dir, GameExe));

    static IEnumerable<string> SteamLibraries()
    {
        var roots = new List<string>();
        foreach (var (hive, key, value) in new[]
                 {
                     (Registry.CurrentUser, @"Software\Valve\Steam", "SteamPath"),
                     (Registry.LocalMachine, @"SOFTWARE\WOW6432Node\Valve\Steam", "InstallPath"),
                     (Registry.LocalMachine, @"SOFTWARE\Valve\Steam", "InstallPath"),
                 })
        {
            try
            {
                if (hive.OpenSubKey(key)?.GetValue(value) is string p && Directory.Exists(p)) roots.Add(Path.GetFullPath(p));
            }
            catch { /* no access */ }
        }

        var libs = new List<string>(roots);
        foreach (var root in roots)
        {
            var vdf = Path.Combine(root, "steamapps", "libraryfolders.vdf");
            if (!File.Exists(vdf)) continue;
            foreach (Match m in Regex.Matches(File.ReadAllText(vdf), "\"path\"\\s+\"([^\"]+)\""))
                libs.Add(m.Groups[1].Value.Replace(@"\\", @"\"));
        }
        return libs.Distinct(StringComparer.OrdinalIgnoreCase);
    }

    public static PartState BepInExState(string game) =>
        File.Exists(Path.Combine(game, "BepInEx", "core", "BepInEx.Unity.IL2CPP.dll")) && File.Exists(Path.Combine(game, "winhttp.dll"))
            ? PartState.Installed : PartState.Missing;

    public static PartState PluginState(string game)
    {
        var path = Path.Combine(game, "BepInEx", "plugins", PluginFile);
        if (!File.Exists(path))
            return File.Exists(Path.Combine(game, "BepInEx", "plugins", LegacyPluginFile)) ? PartState.Outdated : PartState.Missing;
        try
        {
            // Compare by version: never downgrade a newer installed plugin (e.g. from a newer EXE)
            var installed = AssemblyName.GetAssemblyName(path).Version;
            var embedded = EmbeddedPluginVersion();
            if (installed != null && embedded != null && installed != embedded)
                return installed > embedded ? PartState.Installed : PartState.Outdated;

            using var sha = SHA256.Create();
            return sha.ComputeHash(File.ReadAllBytes(path)).SequenceEqual(sha.ComputeHash(EmbeddedPlugin()))
                ? PartState.Installed : PartState.Outdated;
        }
        catch
        {
            return PartState.Outdated;
        }
    }

    static Version? EmbeddedPluginVersion()
    {
        var tmp = Path.Combine(Path.GetTempPath(), "dwno-evolution-helper-plugin-check.dll");
        try
        {
            File.WriteAllBytes(tmp, EmbeddedPlugin());
            return AssemblyName.GetAssemblyName(tmp).Version;
        }
        catch
        {
            return null;
        }
        finally
        {
            try { File.Delete(tmp); } catch { /* doesn't matter */ }
        }
    }

    public static bool GameRunning() =>
        Process.GetProcessesByName(Path.GetFileNameWithoutExtension(GameExe)).Any();

    static byte[] EmbeddedPlugin()
    {
        using var s = Assembly.GetExecutingAssembly().GetManifestResourceStream(PluginFile)!;
        using var ms = new MemoryStream();
        s.CopyTo(ms);
        return ms.ToArray();
    }

    /// Checks whether we may write to the game folder without admin rights.
    public static bool CanWrite(string game)
    {
        try
        {
            var probe = Path.Combine(game, ".evohelfer-test");
            File.WriteAllText(probe, "");
            File.Delete(probe);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static async Task Install(string game, Action<string> progress)
    {
        if (BepInExState(game) != PartState.Installed)
        {
            byte[] zip;
            // If a BepInEx zip sits next to the EXE, it is used instead of the download
            var local = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "BepInEx-Unity.IL2CPP-win-x64*.zip").FirstOrDefault();
            if (local != null)
            {
                zip = File.ReadAllBytes(local);
            }
            else
            {
                progress(Loc.T("setup.download"));
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
                using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
                zip = await http.GetByteArrayAsync(BepInExUrl);
            }

            progress(Loc.T("setup.extract"));
            await Task.Run(() => Extract(zip, game));
        }

        var plugins = Path.Combine(game, "BepInEx", "plugins");
        Directory.CreateDirectory(plugins);
        File.WriteAllBytes(Path.Combine(plugins, PluginFile), EmbeddedPlugin());
        // the old plugin file would otherwise be loaded as a second plugin
        var legacy = Path.Combine(plugins, LegacyPluginFile);
        if (File.Exists(legacy)) File.Delete(legacy);
        HideConsole(game);
    }

    static string ConfigPath(string game) => Path.Combine(game, "BepInEx", "config", "BepInEx.cfg");

    /// Is the black BepInEx console window turned off?
    public static bool ConsoleHidden(string game)
    {
        var cfg = ConfigPath(game);
        if (!File.Exists(cfg)) return false;
        var m = Regex.Match(File.ReadAllText(cfg), @"\[Logging\.Console\][^\[]*?^Enabled\s*=\s*(\w+)",
            RegexOptions.Multiline | RegexOptions.IgnoreCase);
        return m.Success && m.Groups[1].Value.Equals("false", StringComparison.OrdinalIgnoreCase);
    }

    /// Turns off the BepInEx console window. If the config does not exist yet (before the first game start),
    /// it is created with just this entry – BepInEx fills in the rest itself.
    public static void HideConsole(string game)
    {
        var cfg = ConfigPath(game);
        Directory.CreateDirectory(Path.GetDirectoryName(cfg)!);
        if (!File.Exists(cfg))
        {
            File.WriteAllText(cfg, "[Logging.Console]\r\n\r\nEnabled = false\r\n");
            return;
        }

        var text = File.ReadAllText(cfg);
        var section = Regex.Match(text, @"\[Logging\.Console\][^\[]*", RegexOptions.IgnoreCase);
        if (!section.Success)
        {
            File.WriteAllText(cfg, text.TrimEnd() + "\r\n\r\n[Logging.Console]\r\n\r\nEnabled = false\r\n");
            return;
        }
        var fixedSection = Regex.Replace(section.Value, @"^Enabled\s*=\s*\w+", "Enabled = false", RegexOptions.Multiline);
        if (fixedSection == section.Value && !Regex.IsMatch(section.Value, @"^Enabled\s*=", RegexOptions.Multiline))
            fixedSection = section.Value.TrimEnd() + "\r\nEnabled = false\r\n\r\n";
        File.WriteAllText(cfg, text.Substring(0, section.Index) + fixedSection + text.Substring(section.Index + section.Length));
    }

    static void Extract(byte[] zip, string game)
    {
        var root = Path.GetFullPath(game).TrimEnd('\\') + "\\";
        using var archive = new ZipArchive(new MemoryStream(zip));
        foreach (var entry in archive.Entries)
        {
            var dest = Path.GetFullPath(Path.Combine(root, entry.FullName));
            if (!dest.StartsWith(root, StringComparison.OrdinalIgnoreCase)) continue; // no path outside the game folder
            if (entry.FullName.EndsWith("/"))
            {
                Directory.CreateDirectory(dest);
                continue;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            entry.ExtractToFile(dest, true);
        }
    }

    public static void RemovePlugin(string game)
    {
        foreach (var file in new[] { PluginFile, LegacyPluginFile })
        {
            var path = Path.Combine(game, "BepInEx", "plugins", file);
            if (File.Exists(path)) File.Delete(path);
        }
    }

    /// Restarts the EXE with admin rights for the given step and waits for it.
    public static async Task<bool> RunElevated(string action, string game)
    {
        try
        {
            var p = Process.Start(new ProcessStartInfo
            {
                FileName = Assembly.GetExecutingAssembly().Location,
                Arguments = $"--{action} \"{game}\" --lang {Loc.Lang}",
                Verb = "runas",
                UseShellExecute = true,
            });
            if (p == null) return false;
            await Task.Run(() => p.WaitForExit());
            return p.ExitCode == 0;
        }
        catch
        {
            return false; // UAC declined
        }
    }
}
