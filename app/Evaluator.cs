using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace DwnoEvoHelper;

public sealed class Chip
{
    public string Icon { get; set; } = "";
    public Visibility IconVisibility => Icon == "" ? Visibility.Collapsed : Visibility.Visible;
    public string Label { get; set; } = "";
    public string Text { get; set; } = "";
    public string Tooltip { get; set; } = "";
    public string Short { get; set; } = "";
    public Brush Foreground { get; set; } = Brushes.White;
    public Brush Background { get; set; } = Brushes.Transparent;
}

public sealed class Stat
{
    public string Label { get; set; } = "";
    public string Value { get; set; } = "";
}

/// Digimon mit Bild (für Karten, Weg-Anzeige und Suchliste)
public class Pic
{
    public uint Id { get; set; }
    public string Name { get; set; } = "";
    public ImageSource? Image { get; set; }
    public string Initial => Name.Length > 0 ? Name.Substring(0, 1) : "?";
    public Visibility ImageVisibility => Image != null ? Visibility.Visible : Visibility.Collapsed;
    public Visibility InitialVisibility => Image == null ? Visibility.Visible : Visibility.Collapsed;
    public Visibility ArrowVisibility { get; set; } = Visibility.Collapsed;

    public static Pic Of(GameDigimon d) => new() { Id = d.Id, Name = d.Name, Image = Icons.Get(d.Icon) };
}

/// Zeile im Digidex
public sealed class DexRow : Pic
{
    public string Stage { get; set; } = "";
    public string Summary { get; set; } = "";
}

public sealed class EvoCard : Pic
{
    public string Badge { get; set; } = "";
    public Visibility BadgeVisibility => Badge == "" ? Visibility.Collapsed : Visibility.Visible;
    public string PointsText { get; set; } = "";
    public Brush PointsBrush { get; set; } = Brushes.White;
    public double Progress { get; set; }
    public Brush Border { get; set; } = Brushes.Transparent;
    public List<Chip> Conditions { get; set; } = new();
    public string Hint { get; set; } = "";
    public Visibility HintVisibility => Hint == "" ? Visibility.Collapsed : Visibility.Visible;
    public bool Ready { get; set; }
    public int Met { get; set; }
    public string Missing { get; set; } = "";
    public string Fragile { get; set; } = "";       // erfüllte Bedingungen knapp an einer Grenze
    public string PointsTooltip { get; set; } = "";
    public string PriorityText { get; set; } = "";       // Priorität laut Spiel (EvolutionPoint)
    public int? Priority { get; set; }
    public bool KeyIsCurrent { get; set; }               // Schlüssel-Digimon = aktuelle Form des Partners
    public Visibility PriorityVisibility => PriorityText == "" ? Visibility.Collapsed : Visibility.Visible;
    public int Needed { get; set; }
    public Visibility HeaderVisibility { get; set; } = Visibility.Visible;
}

/// Wertet Digitationsbedingungen genauso aus wie das Spiel (PartnerCtrl.ComparisonEvolution):
/// 999999 = keine Bedingung, Wert ≥ 0 = Mindestwert, Wert &lt; 0 = Höchstwert |Wert|.
public static class Evaluator
{
    public const int NoCondition = 999999;

    static readonly (string Key, string Label)[] Keys =
    {
        ("hp", "HP"), ("mp", "MP"), ("str", "STR"), ("sta", "STA"), ("wis", "WIS"), ("spd", "SPD"),
        ("weight", "weight"), ("mistakes", "mistakes"), ("bond", "bond"), ("discipline", "discipline"), ("wins", "wins"),
    };

    public static string Label(string key) => key.All(char.IsUpper) ? key : Loc.T(key);

    public static string Stage(int growth) => growth >= 0 && growth <= 6 ? Loc.T("stage" + growth) : "";

    static Brush B(string key) => (Brush)Application.Current.Resources[key];

    public static bool Meets(int current, int req) => req >= 0 ? current >= req : current <= -req;

    static string N(int v) => v.ToString("N0", Loc.Culture);

    /// Baut die Karte für ein Ziel-Digimon.
    /// liveCmp: Ergebnis der spieleigenen Prüfung pro Bedingung (falls vorhanden).
    /// value == null: neutrale Anzeige (nur Anforderungen, ohne ✓/✗) – für den Digidex.
    public static EvoCard Build(GameData data, GameDigimon target, Func<string, int>? value,
        ICollection<uint> history, IDictionary<string, int>? liveCmp, bool firstReady)
    {
        if (value == null) return Requirements(data, target);
        var chips = new List<(Chip Chip, bool Met, double Gap)>();
        var fragile = new List<string>();

        foreach (var (key, labelKey) in Keys)
        {
            if (!target.Evo.TryGetValue(key, out var req) || req == NoCondition) continue;
            var cur = value(key);
            var label = Label(labelKey);

            bool met = Meets(cur, req);
            if (liveCmp != null)
            {
                var cmpKey = key == "mistakes" && !liveCmp.ContainsKey("mistakes") ? "mistakes_tf" : key;
                if (liveCmp.TryGetValue(cmpKey, out var c)) met = c == 1;
            }

            string text, rule, state;
            double gap; // relativer Abstand – zum Finden der "nächsten" Bedingung
            if (req >= 0)
            {
                text = $"{N(cur)} / {N(req)}";
                rule = Loc.F("min", N(req));
                state = met ? Loc.T("met") : Loc.F("missing", N(req - cur));
                gap = met ? 0 : (req - cur) / (double)Math.Max(req, 1);
            }
            else
            {
                text = $"{N(cur)} / {Loc.T("maxShort")} {N(-req)}";
                rule = Loc.F("max", N(-req));
                state = met ? Loc.T("met") : Loc.F("toomuch", N(cur + req));
                gap = met ? 0 : (cur + req) / (double)Math.Max(-req, 1);
            }
            chips.Add((MakeChip(label, text, $"{label}: {rule} – {state}", $"{label} {state}", met, overMax: !met && req < 0), met, gap));
            if (met && (req < 0 || key == "weight") && Math.Abs(Math.Abs(req) - cur) <= Math.Max(1, Math.Abs(req) * 0.15))
                fragile.Add($"{label} {text}");
        }

        var keys = target.Keys.Select(k => data[k]).Where(k => k != null && GameData.IsValidName(k.Name)).Select(k => k!).ToList();
        if (keys.Count > 0)
        {
            bool met = keys.Any(k => history.Contains(k.Id));
            var names = string.Join(" / ", keys.Select(k => k.Name));
            chips.Add((MakeChip(Loc.T("key"), names, met ? Loc.T("key.met") : Loc.F("key.need", names),
                Loc.F("key.short", names), met), met, met ? 0 : 1));
        }

        int metCount = chips.Count(c => c.Met);
        int needed = Math.Min(target.Matches, chips.Count);
        bool ready = metCount >= needed && needed > 0;

        string hint = "", missingText = "";
        if (!ready)
        {
            var missing = needed - metCount;
            var easiest = chips.Where(c => !c.Met).OrderBy(c => c.Gap).Take(missing).Select(c => c.Chip.Short).ToList();
            hint = Loc.F("hint", missing, string.Join(", ", easiest));
            missingText = string.Join(", ", easiest);
        }

        return new EvoCard
        {
            Id = target.Id,
            Name = target.Name,
            Image = Icons.Get(target.Icon),
            Badge = ready ? Loc.T("badge.met") : "",
            PointsText = $"{metCount} / {needed}",
            PointsBrush = ready ? B("Good") : metCount >= needed - 1 ? B("Accent") : B("NeutralInk"),
            Progress = needed == 0 ? 0 : Math.Min(1, metCount / (double)needed),
            Border = ready ? B("PositiveLine") : B("Line"),
            Conditions = chips.Select(c => c.Chip).ToList(),
            Hint = hint,
            Ready = ready,
            Met = metCount,
            Missing = missingText,
            Fragile = string.Join(", ", fragile),
            Needed = needed,
        };
    }

    /// Nur die Anforderungen eines Digimon (ohne Vergleich mit einem Partner)
    public static EvoCard Requirements(GameData data, GameDigimon target)
    {
        var chips = new List<Chip>();
        foreach (var (key, labelKey) in Keys)
        {
            if (!target.Evo.TryGetValue(key, out var req) || req == NoCondition) continue;
            var label = Label(labelKey);
            chips.Add(Neutral(label, req >= 0 ? "≥ " + N(req) : "≤ " + N(-req),
                $"{label}: {(req >= 0 ? Loc.F("min", N(req)) : Loc.F("max", N(-req)))}"));
        }
        var keys = target.Keys.Select(k => data[k]).Where(k => k != null && GameData.IsValidName(k.Name)).Select(k => k!).ToList();
        if (keys.Count > 0)
        {
            var names = string.Join(" / ", keys.Select(k => k.Name));
            chips.Add(Neutral(Loc.T("key"), names, Loc.F("key.need", names)));
        }

        bool none = target.Matches == NoCondition || chips.Count == 0;
        return new EvoCard
        {
            Id = target.Id,
            Name = target.Name,
            Image = Icons.Get(target.Icon),
            PointsText = none ? "" : Loc.F("dex.needed", Math.Min(target.Matches, chips.Count), chips.Count),
            PointsBrush = B("NeutralInk"),
            Border = B("Line"),
            Conditions = chips,
            Hint = none ? Loc.T(target.Growth == 0 ? "dex.start" : "plan.special") : "",
        };
    }

    /// Kurzfassung der Anforderungen für die Digidex-Liste
    public static string Summary(GameData data, GameDigimon d)
    {
        bool hasConditions = d.Evo.Any(e => e.Value != NoCondition) || d.Keys.Any(k => data[k] != null && GameData.IsValidName(data[k]!.Name));
        if (d.Matches == NoCondition || !hasConditions)
            return Loc.T(d.Growth <= 1 ? "dex.noreq.short" : "dex.special.short");
        var parts = new List<string>();
        foreach (var (key, labelKey) in Keys)
        {
            if (!d.Evo.TryGetValue(key, out var req) || req == NoCondition) continue;
            parts.Add($"{Label(labelKey)} {(req >= 0 ? "≥" : "≤")}{N(Math.Abs(req))}");
        }
        var keys = d.Keys.Select(k => data[k]).Where(k => k != null && GameData.IsValidName(k.Name)).Select(k => k!.Name).ToList();
        if (keys.Count > 0) parts.Add($"{Loc.T("key")} {string.Join("/", keys)}");
        return string.Join(" · ", parts) + "   —   " + Loc.F("dex.needed", Math.Min(d.Matches, parts.Count), parts.Count);
    }

    static Chip Neutral(string label, string text, string tip) => new()
    {
        Icon = "",
        Label = label,
        Text = text,
        Tooltip = tip,
        Foreground = B("NeutralInk"),
        Background = B("NeutralBg"),
    };

    /// erfüllt = Grün-Paar; offen = neutral; über einem Höchstwert (muss gesenkt werden) = Rot-Paar
    static Chip MakeChip(string label, string text, string tip, string shortText, bool met, bool overMax = false) => new()
    {
        Icon = met ? "✓" : "✗",
        Label = label,
        Text = text,
        Tooltip = tip,
        Short = shortText,
        Foreground = met ? B("Good") : overMax ? B("Bad") : B("NeutralInk"),
        Background = met ? B("GoodBg") : overMax ? B("BadBg") : B("NeutralBg"),
    };
}
