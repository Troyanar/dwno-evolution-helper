using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace DwnoEvoHelper;

/// An entry from evolog.jsonl: what the partner looked like right before the evolution (written by the plugin).
[DataContract]
public sealed class EvoLogEntry
{
    [DataMember(Name = "time")] public string? TimeText { get; set; }
    [DataMember(Name = "slot")] public int Slot { get; set; }
    [DataMember(Name = "from")] public string? From { get; set; }
    [DataMember(Name = "to")] public string? To { get; set; }
    [DataMember(Name = "before")] public LivePartner? Before { get; set; }
}

/// Evaluation of an evolution: what each rule would have predicted and what actually happened.
public sealed class EvoLogResult
{
    public EvoLogEntry Entry = null!;
    public List<EvoCard> Cards = new();
    public int ReadyCount;
    public string? Surplus, Priority, ListOrder, Choice, Fallback;   // predicted names
    public bool Matches(string? predicted) => predicted != null && predicted == Entry.To;
}

public static class EvoLog
{
    public static string LogPath => Path.Combine(GameFiles.Dir, "evolog.jsonl");
    public static string ReportPath => Path.Combine(GameFiles.Dir, "digitationen.txt");

    public static List<EvoLogResult> Load(GameData data)
    {
        var results = new List<EvoLogResult>();
        if (!File.Exists(LogPath)) return results;

        string[] lines;
        try
        {
            using var fs = new FileStream(LogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using var sr = new StreamReader(fs, Encoding.UTF8);
            lines = sr.ReadToEnd().Split('\n');
        }
        catch
        {
            return results;
        }

        foreach (var line in lines.Where(l => l.Trim().Length > 0))
        {
            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(line));
            var e = Json.Read<EvoLogEntry>(ms);
            if (e?.Before == null) continue;
            e.Before.Values ??= new();
            e.Before.Evolutions ??= new();
            e.Before.History ??= new();

            e.From = Loc.DigimonName(e.From ?? "?", data.Lang);
            e.To = Loc.DigimonName(e.To ?? "?", data.Lang);
            var cards = EvoRules.Cards(data, e.Before, useLiveCmp: true);
            results.Add(new EvoLogResult
            {
                Entry = e,
                Cards = cards,
                ReadyCount = cards.Count(c => c.Ready),
                Surplus = EvoRules.BySurplus(cards)?.Name,
                Priority = EvoRules.ByPriority(cards)?.Name,
                ListOrder = EvoRules.ByListOrder(cards)?.Name,
                Choice = e.Before.Choice?.Valid == true ? e.Before.Choice.Name : null,
                Fallback = EvoRules.ClosestFallback(cards)?.Name,
            });
        }
        return results;
    }

    /// Readable report (for analysis outside the app)
    public static void WriteReport(List<EvoLogResult> results)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Digimon World Next Order Evolution Helper – digivolution log");
        sb.AppendLine($"As of {DateTime.Now:yyyy-MM-dd HH:mm}, {results.Count} digivolutions");
        sb.AppendLine();
        foreach (var (rule, pick) in Rules())
        {
            var judged = results.Where(r => r.ReadyCount > 0).ToList();
            sb.AppendLine($"{rule,-28} {judged.Count(r => r.Matches(pick(r)))} of {judged.Count} correct" +
                          $" (with several targets met: {judged.Where(r => r.ReadyCount > 1).Count(r => r.Matches(pick(r)))} of {judged.Count(r => r.ReadyCount > 1)})");
        }
        var fb = results.Where(r => r.ReadyCount == 0).ToList();
        sb.AppendLine($"{Loc.T("rule.fallback"),-28} {fb.Count(r => r.Matches(r.Fallback))} of {fb.Count} correct (cases with no target met)");
        sb.AppendLine();
        foreach (var r in results)
        {
            var p = r.Entry.Before!;
            sb.AppendLine($"{r.Entry.TimeText}  {r.Entry.From} → {r.Entry.To}  (age {p.Age})");
            foreach (var c in r.Cards)
                sb.AppendLine($"    {(c.Name == r.Entry.To ? "→" : " ")} {c.Name,-28} {c.Met}/{c.Needed} ({c.Met - c.Needed:+0;-0;+0})" +
                              $"  Prio {c.Priority?.ToString() ?? "–"}{(c.KeyIsCurrent ? "  key=current form" : "")}");
            sb.AppendLine($"    Surplus: {r.Surplus ?? "–"} · Priority: {r.Priority ?? "–"} · List: {r.ListOrder ?? "–"} · Game pick just before: {r.Choice ?? "–"} · Fallback/closest: {r.Fallback ?? "–"}");
            sb.AppendLine("    Values: " + string.Join(", ", p.Values.Select(v => $"{v.Key}={v.Value}")));
            sb.AppendLine();
        }
        try { File.WriteAllText(ReportPath, sb.ToString(), Encoding.UTF8); } catch { /* not critical */ }
    }

    public static IEnumerable<(string Rule, Func<EvoLogResult, string?> Pick)> Rules()
    {
        yield return (Loc.T("rule.surplus"), r => r.Surplus);
        yield return (Loc.T("rule.prio"), r => r.Priority);
        yield return (Loc.T("rule.list"), r => r.ListOrder);
        yield return (Loc.T("rule.choice"), r => r.Choice);
    }
}
