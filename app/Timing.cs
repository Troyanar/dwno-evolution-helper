using System;
using System.Collections.Generic;
using System.Linq;

namespace DwnoEvoHelper;

/// Wann digitiert der Partner? Das Spiel prüft beim Tageswechsel (Alter +1).
/// Die Alters-Fenster sind Richtwerte der Community (nicht aus den Spieldaten auslesbar):
/// Champion ab Alter 4 (eigene Beobachtung), Ultra ca. 7–8, Mega ca. 10–11; Rookies ohne erfüllte Bedingung
/// werden spätestens mit Alter 7 zu Numemon (STR > WIS) bzw. Geremon.
public static class Timing
{
    static readonly Dictionary<int, (int From, int To)> Window = new()
    {
        [2] = (4, 4),   // Rookie → Champion (beobachtet: beide Partner erst mit Alter 4, obwohl vorher erfüllt)
        [3] = (7, 8),   // Champion → Ultra
        [4] = (10, 11), // Ultra → Mega
    };

    const int RookieDeadline = 7;

    /// Minuten (Spielzeit) pro Tag
    static double DayMinutes(WorldInfo? w) => (w?.HoursPerDay > 0 ? w.HoursPerDay : 24) * 60;

    // Wie schnell läuft die Spieluhr? Gemessen aus den Live-Daten (Spielminuten pro echter Sekunde).
    // Gemessen ~0,24; die Uhr steht in Menüs und Gesprächen, daher nur als "ca." verwendet.
    static double _rate;
    public static bool HasRate => _rate > 0.01;
    static double _lastGameMinute = -1;
    static DateTime _lastWall;

    public static void Observe(WorldInfo? w, DateTime stamp)
    {
        if (w == null) return;
        double m = w.TotalDay * DayMinutes(w) + w.Hour * 60 + w.Minute;
        if (_lastGameMinute >= 0 && m == _lastGameMinute) return; // Uhr springt nur minutenweise: Basis halten

        if (_lastGameMinute >= 0)
        {
            double dm = m - _lastGameMinute, ds = (stamp - _lastWall).TotalSeconds;
            if (dm > 0 && dm < 60 && ds > 0 && ds < 60)
                _rate = _rate <= 0 ? dm / ds : _rate * 0.8 + dm / ds * 0.2;
        }
        _lastGameMinute = m;
        _lastWall = stamp;
    }

    /// Spielminuten bis zum nächsten Alterssprung (das Alter steigt täglich zur selben Spieluhrzeit).
    static double? MinutesToNextDay(LivePartner p, WorldInfo? w)
    {
        if (w == null || p.TimeFromAge < 0) return null;
        var left = DayMinutes(w) - p.TimeFromAge;
        return left < 0 || left > DayMinutes(w) ? null : left;
    }

    /// "um 09:00 (ca. 1 h 38 min)" – Spieluhrzeit des nächsten Alterssprungs, dazu die geschätzte echte Zeit
    static string? NextDayText(LivePartner p, WorldInfo? w)
    {
        if (MinutesToNextDay(p, w) is not double left) return null;
        int now = w!.Hour * 60 + w.Minute;
        int at = (int)Math.Round((now + left) % DayMinutes(w));
        var clock = $"{at / 60:00}:{at % 60:00}";
        return _rate > 0.01
            ? Loc.F("timing.nextday.real", clock, Duration(TimeSpan.FromSeconds(left / _rate)))
            : Loc.F("timing.nextday", clock);
    }

    /// Tage bis zum Beginn des nächsten Alters-Fensters (0 = schon erreicht), null bei Endstufe/Baby
    public static int? DaysUntilWindow(LivePartner p) =>
        Window.TryGetValue(p.Growth, out var win) ? Math.Max(0, win.From - p.Age) : (int?)null;

    /// Verbleibende Lebenszeit in Tagen
    public static double? LifeDays(LivePartner p, WorldInfo? w) =>
        p.Lifetime > 0 ? p.Lifetime / DayMinutes(w) : (double?)null;

    public static string Describe(LivePartner p, WorldInfo? w, bool anyReady)
    {
        var parts = new List<string>();
        var next = NextDayText(p, w);

        if (p.Growth <= 1)
        {
            parts.Add(Loc.T("timing.soon"));
        }
        else if (Window.TryGetValue(p.Growth, out var win))
        {
            if (p.Age < win.From)
            {
                int a = win.From - p.Age, b = win.To - p.Age;
                parts.Add(Loc.F("timing.window", Range(win.From, win.To), Range(a, b)));
            }
            else
            {
                parts.Add(Loc.T(anyReady ? "timing.ready" : "timing.now"));
            }
        }
        else
        {
            return Loc.T("timing.final");
        }

        if (next != null) parts.Add(next);

        // Rookie ohne erfüllte Bedingung: Fehl-Digitation zum Stichtag
        if (p.Growth == 2 && !anyReady && p.Age < RookieDeadline)
        {
            var fallback = p.Value("str") > p.Value("wis") ? "Numemon" : "Geremon";
            parts.Add(Loc.F("timing.fallback", RookieDeadline, fallback));
        }
        return string.Join(" · ", parts);
    }

    static string Range(int a, int b) => a == b ? a.ToString() : $"{a}–{b}";

    public static string Duration(TimeSpan t) =>
        t.TotalMinutes < 1 ? Loc.T("timing.lessmin")
        : t.TotalHours < 1 ? Loc.F("timing.min", (int)Math.Ceiling(t.TotalMinutes))
        : Loc.F("timing.hmin", (int)t.TotalHours, t.Minutes);
}
