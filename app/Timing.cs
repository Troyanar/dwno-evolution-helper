using System;
using System.Collections.Generic;
using System.Linq;

namespace DwnoEvoHelper;

/// When does the partner evolve? The game checks at the day change (age +1).
/// The age windows are community guidelines (not readable from the game data):
/// Champion from age 4 (own observation), Ultra approx. 7–8, Mega approx. 10–11; Rookies with no condition met
/// become Numemon (STR > WIS) or Geremon at age 7 at the latest.
public static class Timing
{
    static readonly Dictionary<int, (int From, int To)> Window = new()
    {
        [2] = (4, 4),   // Rookie → Champion (observed: both partners only at age 4, although met earlier)
        [3] = (7, 8),   // Champion → Ultra
        [4] = (10, 11), // Ultra → Mega
    };

    const int RookieDeadline = 7;

    /// Minutes (game time) per day
    static double DayMinutes(WorldInfo? w) => (w?.HoursPerDay > 0 ? w.HoursPerDay : 24) * 60;

    // How fast does the game clock run? Measured from the live data (game minutes per real second).
    // Measured ~0.24; the clock stops in menus and dialogues, so it is only used as "approx.".
    static double _rate;
    public static bool HasRate => _rate > 0.01;
    static double _lastGameMinute = -1;
    static DateTime _lastWall;

    public static void Observe(WorldInfo? w, DateTime stamp)
    {
        if (w == null) return;
        double m = w.TotalDay * DayMinutes(w) + w.Hour * 60 + w.Minute;
        if (_lastGameMinute >= 0 && m == _lastGameMinute) return; // clock only advances in whole minutes: keep the baseline

        if (_lastGameMinute >= 0)
        {
            double dm = m - _lastGameMinute, ds = (stamp - _lastWall).TotalSeconds;
            if (dm > 0 && dm < 60 && ds > 0 && ds < 60)
                _rate = _rate <= 0 ? dm / ds : _rate * 0.8 + dm / ds * 0.2;
        }
        _lastGameMinute = m;
        _lastWall = stamp;
    }

    /// Game minutes until the next age increase (age goes up daily at the same game-clock time).
    static double? MinutesToNextDay(LivePartner p, WorldInfo? w)
    {
        if (w == null || p.TimeFromAge < 0) return null;
        var left = DayMinutes(w) - p.TimeFromAge;
        return left < 0 || left > DayMinutes(w) ? null : left;
    }

    /// "at 09:00 (approx. 1 h 38 min)" – game-clock time of the next age increase, plus the estimated real time
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

    /// Days until the next age window starts (0 = already reached), null for final stage/baby
    public static int? DaysUntilWindow(LivePartner p) =>
        Window.TryGetValue(p.Growth, out var win) ? Math.Max(0, win.From - p.Age) : (int?)null;

    /// Remaining lifetime in days
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

        // Rookie with no condition met: failed evolution on the deadline
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
