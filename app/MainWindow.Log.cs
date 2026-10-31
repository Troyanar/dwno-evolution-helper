using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace DwnoEvoHelper;

public sealed class LogLine
{
    public string Text { get; set; } = "";
    public Brush Brush { get; set; } = Brushes.White;
    public FontWeight Weight { get; set; } = FontWeights.Normal;
}

public sealed class LogEntryVm : Pic
{
    public string Sub { get; set; } = "";
    public List<LogLine> Lines { get; set; } = new();
    public List<LogLine> Verdicts { get; set; } = new();
}

/// Log view: every evolution with the state before it and which rule would have predicted it correctly.
public partial class MainWindow
{
    bool _logMode;
    DateTime _logStamp;

    void RenderLog(bool force = false)
    {
        if (_data == null) return;
        var stamp = File.Exists(EvoLog.LogPath) ? File.GetLastWriteTimeUtc(EvoLog.LogPath) : default;
        if (!force && stamp == _logStamp) return;
        _logStamp = stamp;

        var results = EvoLog.Load(_data);
        EvoLog.WriteReport(results);
        Brush B(string k) => (Brush)FindResource(k);

        LogTitle.Text = Loc.T("log.title");
        LogEmpty.Text = Loc.T("log.empty");
        LogEmpty.Visibility = results.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        // Score per rule – what matters are the cases with several met targets
        var judged = results.Where(r => r.ReadyCount > 0).ToList();
        var multi = judged.Where(r => r.ReadyCount > 1).ToList();
        LogSummary.Text = Loc.F("log.count", results.Count, multi.Count);
        var fallbacks = results.Where(r => r.ReadyCount == 0).ToList();
        var fallbackLine = new LogLine
        {
            Text = Loc.F("log.fallbackscore", Loc.T("rule.fallback"), fallbacks.Count(r => r.Matches(r.Fallback)), fallbacks.Count),
            Brush = fallbacks.Count == 0 ? B("Muted") : fallbacks.All(r => r.Matches(r.Fallback)) ? B("Good") : B("NeutralInk"),
        };
        LogScores.ItemsSource = EvoLog.Rules().Select(x =>
        {
            int ok = judged.Count(r => r.Matches(x.Pick(r))), okMulti = multi.Count(r => r.Matches(x.Pick(r)));
            bool perfect = judged.Count > 0 && ok == judged.Count;
            return new LogLine
            {
                Text = Loc.F("log.score", x.Rule, ok, judged.Count, okMulti, multi.Count),
                Brush = judged.Count == 0 ? B("Muted") : perfect ? B("Good") : B("NeutralInk"),
            };
        }).Append(fallbackLine).ToList();

        LogList.ItemsSource = results.AsEnumerable().Reverse().Select(r =>
        {
            var p = r.Entry.Before!;
            var to = _data.Dex().FirstOrDefault(d => d.Name == r.Entry.To);
            var time = DateTime.TryParse(r.Entry.TimeText, out var t) ? t.ToString("g", Loc.Culture) : r.Entry.TimeText;
            return new LogEntryVm
            {
                Id = to?.Id ?? 0,
                Name = $"{r.Entry.From} → {r.Entry.To}",
                Image = Icons.Get(to?.Icon),
                Sub = $"{time} · {Loc.F("age", p.Age)}",
                Lines = r.Cards.Select(c => new LogLine
                {
                    Text = $"{(c.Name == r.Entry.To ? "→ " : "   ")}{c.Name}  {c.Met}/{c.Needed} ({(c.Met - c.Needed):+0;-0;+0})"
                           + (c.Priority != null ? $" · {Loc.F("prio.short", c.Priority.Value.ToString("N0", Loc.Culture))}" : "")
                           + (c.KeyIsCurrent ? " · " + Loc.T("log.keycurrent") : ""),
                    Brush = c.Name == r.Entry.To ? B("Good") : c.Ready ? B("Text") : B("Muted"),
                    Weight = c.Name == r.Entry.To ? FontWeights.SemiBold : FontWeights.Normal,
                }).ToList(),
                Verdicts = r.ReadyCount == 0
                    ? new List<LogLine>
                    {
                        new() { Text = Loc.T("log.none"), Brush = B("Muted") },
                        new()
                        {
                            Text = (r.Matches(r.Fallback) ? "✓ " : "✗ ") + Loc.T("rule.fallback") + ": " + (r.Fallback ?? "–"),
                            Brush = r.Matches(r.Fallback) ? B("Good") : B("Bad"),
                        },
                    }
                    : EvoLog.Rules().Select(x => new LogLine
                    {
                        Text = (r.Matches(x.Pick(r)) ? "✓ " : "✗ ") + x.Rule + ": " + (x.Pick(r) ?? "–"),
                        Brush = r.Matches(x.Pick(r)) ? B("Good") : B("Bad"),
                    }).ToList(),
            };
        }).ToList();
    }

    void OpenReport_Click(object sender, RoutedEventArgs e)
    {
        RenderLog(force: true);
        try { System.Diagnostics.Process.Start(EvoLog.ReportPath); } catch { /* no default program */ }
    }
}
