using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;

namespace DwnoEvoHelper;

/// Trainingsplan aus gemessenen Trainings.
/// Das Spiel speichert die Zuwächse nicht in seinen Datentabellen (sie hängen von Arena-Stufe, Boni und
/// Digimon ab). Deshalb misst die App jede Trainingseinheit selbst: ein Sprung in mindestens zwei Werten,
/// danach 3 s Ruhe. Die Arena ist der Wert mit dem größten (normierten) Zuwachs.
public static class Trainer
{
    public static readonly string[] Stats = { "hp", "mp", "str", "sta", "wis", "spd" };
    const int Keep = 20; // letzte Einheiten je Arena

    [DataContract]
    sealed class Store
    {
        [DataMember] public Dictionary<string, List<int[]>> Sessions { get; set; } = new();
    }

    sealed class Watch
    {
        public uint Id;
        public int[] Baseline = new int[6];
        public int[] Last = new int[6];
        public DateTime LastChange;
        public bool Pending;
    }

    static readonly string StorePath = Path.Combine(GameFiles.Dir, "training.json");
    static Store? _store;
    static readonly Dictionary<int, Watch> Watches = new();

    static Store Data => _store ??= Json.Read<Store>(StorePath) ?? new Store();

    /// HP/MP steigen in Zehnerschritten – für den Vergleich normieren
    static double Scale(int i) => i < 2 ? 10.0 : 1.0;

    public static int SessionCount => Data.Sessions?.Values.Sum(l => l.Count) ?? 0;

    /// Bei jedem neuen Live-Stand aufrufen
    public static void Observe(LiveState? live, DateTime now)
    {
        if (live?.Partners == null) return;
        foreach (var p in live.Partners)
        {
            var cur = Stats.Select(p.Value).ToArray();
            if (!Watches.TryGetValue(p.Slot, out var w) || w.Id != p.Id)
            {
                // neuer Partner oder Digitation: Werte springen, das ist kein Training
                Watches[p.Slot] = new Watch { Id = p.Id, Baseline = cur, Last = cur, LastChange = now };
                continue;
            }

            if (!cur.SequenceEqual(w.Last))
            {
                w.Last = cur;
                w.LastChange = now;
                w.Pending = true;
                continue;
            }

            if (w.Pending && (now - w.LastChange).TotalSeconds >= 3)
            {
                var delta = cur.Select((v, i) => v - w.Baseline[i]).ToArray();
                Record(delta);
                w.Baseline = cur;
                w.Pending = false;
            }
        }
    }

    static void Record(int[] delta)
    {
        // Training: nichts sinkt, mindestens zwei Werte steigen, der Hauptwert deutlich
        if (delta.Any(d => d < 0) || delta.Count(d => d > 0) < 2) return;
        int gym = Enumerable.Range(0, 6).OrderByDescending(i => delta[i] / Scale(i)).First();
        if (delta[gym] / Scale(gym) < 5) return;

        var key = Stats[gym];
        Data.Sessions ??= new();
        if (!Data.Sessions.TryGetValue(key, out var list)) Data.Sessions[key] = list = new List<int[]>();
        list.Add(delta);
        if (list.Count > Keep) list.RemoveAt(0);
        try { Json.Write(StorePath, Data); } catch { /* nicht kritisch */ }
    }

    /// Durchschnittlicher Zuwachs je Arena (aus den gemessenen Einheiten)
    static Dictionary<string, double[]> Averages() =>
        (Data.Sessions ?? new()).Where(kv => kv.Value.Count > 0).ToDictionary(
            kv => kv.Key,
            kv => Enumerable.Range(0, 6).Select(i => kv.Value.Average(v => (double)v[i])).ToArray());

    /// Wie oft welche Arena, um die fehlenden Werte zu erreichen?
    /// need: fehlende Punkte je Wert (hp..spd). Ergebnis: Arena → Anzahl, plus Werte ohne Messung.
    public static (List<(string Gym, int Count)> Plan, List<string> Unmeasured) Plan(double[] need)
    {
        var avg = Averages();
        var rest = (double[])need.Clone();
        var counts = new Dictionary<string, int>();

        for (int step = 0; step < 80 && rest.Any(r => r > 0); step++)
        {
            string? best = null;
            double bestGain = 0;
            foreach (var (gym, gain) in avg.Select(kv => (kv.Key, kv.Value)))
            {
                double g = Enumerable.Range(0, 6).Sum(i => Math.Min(Math.Max(rest[i], 0), Math.Max(gain[i], 0)) / Scale(i));
                if (g > bestGain) { bestGain = g; best = gym; }
            }
            if (best == null) break;
            counts[best] = counts.TryGetValue(best, out var c) ? c + 1 : 1;
            for (int i = 0; i < 6; i++) rest[i] -= avg[best][i];
        }

        var unmeasured = Enumerable.Range(0, 6).Where(i => rest[i] > 0).Select(i => Stats[i]).ToList();
        var plan = counts.OrderByDescending(kv => kv.Value).Select(kv => (kv.Key, kv.Value)).ToList();
        return (plan, unmeasured);
    }

    /// Fehlende Punkte der noch offenen Mindest-Bedingungen eines Ziels (nur Werte, die Training hebt)
    public static double[] Need(GameDigimon target, Func<string, int> value) =>
        Stats.Select(k => target.Evo.TryGetValue(k, out var req) && req != Evaluator.NoCondition && req > 0
            ? Math.Max(0, req - value(k)) : 0.0).ToArray();

    /// "3× WIS, 1× SPD" bzw. Hinweis, wenn eine Arena noch nie gemessen wurde
    public static string Describe(GameDigimon target, Func<string, int> value)
    {
        var need = Need(target, value);
        if (need.All(n => n <= 0)) return "";
        var (plan, unmeasured) = Plan(need);
        var parts = plan.Select(x => $"{x.Count}× {Evaluator.Label(x.Gym.ToUpperInvariant())}").ToList();
        var text = parts.Count > 0 ? Loc.F("train.plan", string.Join(", ", parts)) : "";
        if (unmeasured.Count > 0)
            text += (text != "" ? " · " : "") + Loc.F("train.unmeasured", string.Join(", ", unmeasured.Select(u => u.ToUpperInvariant())));
        return text;
    }
}
