using System.Collections.Generic;

namespace DwnoEvoHelper;

/// In-app changelog (newest first). Keep in sync with CHANGELOG.md in the repository.
public static class Changelog
{
    public sealed class Entry
    {
        public string Version { get; set; } = "";
        public string Date { get; set; } = "";
        public List<string> Items { get; set; } = new();
        public string Title => $"{Version}  ·  {Date}";
    }

    public static readonly List<Entry> Entries = new()
    {
        new() { Version = "1.7.0", Date = "2026-10-31", Items =
        {
            "Auto-detect game folder",
            "Changelog",
        } },
        new() { Version = "1.6.0", Date = "2026-10-31", Items =
        {
            "Renamed to Digimon World Next Order Evolution Helper",
            "English UI by default, language picker",
            "English Digimon names in the English UI",
            "Plugin renamed to DWNO_EvolutionHelper.dll",
        } },
        new() { Version = "1.5.0", Date = "2026-10-31", Items =
        {
            "Training plan",
            "Lifetime and age gap warnings",
            "Hotkeys: Ctrl+Shift+E, Ctrl+Shift+D",
            "Update check",
        } },
        new() { Version = "1.4.0", Date = "2026-10-31", Items =
        {
            "Shows the game's evolution choice",
            "Priority per evolution",
            "Evolution log",
        } },
        new() { Version = "1.3.0", Date = "2026-10-31", Items =
        {
            "Evolution timing and lifetime",
            "Warning for borderline conditions",
        } },
        new() { Version = "1.0.0", Date = "2026-10-31", Items =
        {
            "Initial release",
        } },
    };
}
