using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace DwnoEvoHelperPlugin;

[BepInPlugin("troyanar.dwno.evolutionhelper", "DWNO Evolution Helper", Version)]
public class Plugin : BasePlugin
{
    public const string Version = "1.5.0";

    internal static ManualLogSource Logger;

    public override void Load()
    {
        Logger = Log;
        new Harmony("troyanar.dwno.evolutionhelper").PatchAll(typeof(EvoExporter));
        Logger.LogInfo("DWNO Evolution Helper loaded – writing to " + EvoExporter.OutDir);
    }
}

/// Reads the partner data every second and writes it as JSON for the helper app.
/// Hooks into AppMainScript.Update, so it runs on the Unity main thread – where game
/// functions can be called safely.
public static class EvoExporter
{
    public static readonly string OutDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DWNO-EvoHelper");

    static readonly JsonSerializerOptions Json = new() { WriteIndented = false };

    static DateTime _next;
    static string _tableLang;
    static Dictionary<string, object> _cmpTest;
    static readonly HashSet<string> _loggedErrors = new();

    [HarmonyPostfix]
    [HarmonyPatch(typeof(AppMainScript), nameof(AppMainScript.Update))]
    static void Update()
    {
        // Note: the plugin deliberately loads none of the game's asset bundles – an earlier attempt corrupted
        // the game's asset cache. The images ship with the app.

        var now = DateTime.UtcNow;
        if (now < _next) return;
        _next = now.AddSeconds(1);

        try
        {
            var lang = CurrentLanguage();
            if (_tableLang != lang && WriteGameData(lang)) _tableLang = lang;
            WriteLive(lang);
        }
        catch (Exception e)
        {
            LogOnce("Tick", e);
        }
    }

    internal static void LogOnce(string where, Exception e)
    {
        if (_loggedErrors.Add(where + e.GetType().Name + e.Message))
            Plugin.Logger.LogWarning($"{where}: {e}");
    }

    static string CurrentLanguage()
    {
        try { return Language.GetLanguageFileCode() ?? ""; }
        catch { return ""; }
    }

    /// Placeholder entries are named "ランゲージが見つかりません！！" (language not found).
    internal static bool IsValidName(string name) =>
        !string.IsNullOrWhiteSpace(name) && name != ParameterDigimonData.INVALID_PARAM_NAME &&
        !name.Contains("ランゲージ") && !name.Contains("見つかりません");

    // ---------- Master data of all Digimon (incl. evolution conditions) ----------

    static bool WriteGameData(string lang)
    {
        var pm = AppMainScript.parameterManager;
        var table = pm?.digimonData;
        if (table == null || !table.IsBinded()) return false;

        var list = new List<object>();
        foreach (var p in table.GetParams())
        {
            if (p == null || p.m_id == 0) continue;
            string name;
            try { name = p.GetDefaultName(); } catch { continue; }
            if (!IsValidName(name)) continue;

            var icon = IconCode(p.m_mdlName);
            list.Add(new Dictionary<string, object>
            {
                ["id"] = p.m_id,
                ["name"] = name,
                ["icon"] = icon,
                ["growth"] = p.m_growth,
                ["evo"] = Conditions(p),
                ["keys"] = NonZero(p.m_key1, p.m_key2, p.m_key3),
                ["matches"] = p.m_evo_matches,
                ["to"] = NonZero(p.m_evolution1, p.m_evolution2, p.m_evolution3, p.m_evolution4, p.m_evolution5),
                ["from"] = NonZero(p.m_beforeID1, p.m_beforeID2, p.m_beforeID3, p.m_beforeID4, p.m_beforeID5),
                ["flags"] = p.m_flags,
                ["dlc"] = p.m_dlc_no,
                ["lifetime"] = p.m_lifetime,
            });
        }
        if (list.Count == 0) return false;

        WriteAtomic("gamedata.json", new Dictionary<string, object>
        {
            ["version"] = 2,
            ["plugin"] = Plugin.Version,
            ["lang"] = lang,
            ["created"] = DateTime.Now.ToString("s"),
            ["digimon"] = list,
        });
        Plugin.Logger.LogInfo($"Digimon table exported ({list.Count} entries, language {lang})");
        return true;
    }

    /// Icon code from the model name: "chr_a001" → "a001" (name of the images in the app)
    static string IconCode(string modelName)
    {
        var m = System.Text.RegularExpressions.Regex.Match(modelName ?? "", @"([a-zA-Z])(\d{3})(?!\d)");
        return m.Success ? (m.Groups[1].Value + m.Groups[2].Value).ToLowerInvariant() : null;
    }

    static Dictionary<string, int> Conditions(ParameterDigimonData p) => new()
    {
        ["hp"] = p.m_evo_hpmax,
        ["mp"] = p.m_evo_mpmax,
        ["str"] = p.m_evo_forcefulness,
        ["sta"] = p.m_evo_robustness,
        ["wis"] = p.m_evo_cleverness,
        ["spd"] = p.m_evo_rapidity,
        ["weight"] = p.m_evo_weight,
        ["mistakes"] = p.m_evo_mistakes,
        ["bond"] = p.m_evo_bounds,
        ["discipline"] = p.m_evo_education,
        ["wins"] = p.m_evo_battlewin,
    };

    static List<uint> NonZero(params uint[] ids)
    {
        var l = new List<uint>();
        foreach (var id in ids)
            if (id != 0 && id != uint.MaxValue) l.Add(id);
        return l;
    }

    // ---------- Live data of the partners ----------

    static void WriteLive(string lang)
    {
        var root = new Dictionary<string, object>
        {
            ["version"] = 2,
            ["plugin"] = Plugin.Version,
            ["lang"] = lang,
            ["time"] = DateTime.Now.ToString("o"),
            ["pid"] = Environment.ProcessId,
        };

        var player = StorageData.m_playerData;
        if (player == null)
        {
            root["inGame"] = false;
            WriteAtomic("live.json", root);
            return;
        }

        bool inField = false;
        try { inField = MainGameManager.Ref != null; } catch { }

        var partners = new List<object>();
        for (int i = 0; i < 2; i++)
        {
            try
            {
                var p = ReadPartner(i, inField);
                if (p != null) partners.Add(p);
            }
            catch (Exception e)
            {
                LogOnce("Partner" + i, e);
            }
        }

        root["inGame"] = partners.Count > 0;
        root["partners"] = partners;
        try { root["world"] = ReadWorld(); } catch (Exception e) { LogOnce("World", e); }
        if (_cmpTest != null) root["cmpTest"] = _cmpTest;
        WriteAtomic("live.json", root);
        try { LogEvolutions(partners, root.TryGetValue("world", out var w) ? w : null); } catch (Exception e) { LogOnce("EvoLog", e); }
    }

    // ---------- Evolution log ----------
    // Remembers the last state per partner (incl. the game's check of each condition and priority).
    // When the form changes, the state from just before is written to evolog.jsonl together with the result.

    static readonly Dictionary<int, Dictionary<string, object>> _lastSnapshot = new();

    static void LogEvolutions(List<object> partners, object world)
    {
        foreach (Dictionary<string, object> p in partners)
        {
            int slot = (int)p["slot"];
            if (_lastSnapshot.TryGetValue(slot, out var before) && !Equals(before["id"], p["id"]))
            {
                var entry = new Dictionary<string, object>
                {
                    ["time"] = DateTime.Now.ToString("o"),
                    ["slot"] = slot,
                    ["from"] = before["name"],
                    ["to"] = p["name"],
                    ["world"] = world,
                    ["before"] = before,
                };
                Directory.CreateDirectory(OutDir);
                File.AppendAllText(Path.Combine(OutDir, "evolog.jsonl"), JsonSerializer.Serialize(entry, Json) + "\n");
                Plugin.Logger.LogInfo($"Digivolution logged: {before["name"]} → {p["name"]}");
            }
            _lastSnapshot[slot] = p;
        }
    }

    /// Game clock: time of day, day counter and how long one game hour lasts in real seconds.
    static Dictionary<string, object> ReadWorld()
    {
        var w = StorageData.m_worldData;
        if (w == null) return null;
        return new Dictionary<string, object>
        {
            ["hour"] = w.GetHour(),
            ["minute"] = w.GetMinutes(),
            ["time"] = w.m_time,
            ["totalDay"] = w.m_totalDay,
            ["daySpent"] = w.m_daySpent,
            ["year"] = w.m_year,
            ["secPerHour"] = WorldData.SECOND_PER_HOUR_INGAME,
            ["hoursPerDay"] = WorldData.HOUR_PER_DAY,
        };
    }

    static Dictionary<string, object> ReadPartner(int idx, bool inField)
    {
        var data = StorageData.GetPartnerData((AppInfo.PARTNER_NO)idx);
        var c = data?.m_commonData;
        if (c == null || c.m_baseID == 0) return null;

        var param = ParameterDigimonData.GetParam(c.m_baseID);
        var values = new Dictionary<string, int>
        {
            ["hp"] = c.m_hpMax,
            ["mp"] = c.m_mpMax,
            ["str"] = c.m_forcefulness,
            ["sta"] = c.m_robustness,
            ["wis"] = c.m_cleverness,
            ["spd"] = c.m_rapidity,
            ["weight"] = c.m_weight,
            ["mistakes"] = data.m_trainingFailure,
            ["bond"] = data.m_bonds,
            ["discipline"] = data.m_breeding,
            ["wins"] = data.m_battleWin,
            ["curse"] = data.m_curse,
            ["trainingFailure"] = data.m_trainingFailure,
            ["mood"] = data.m_mood,
            ["fatigue"] = data.m_fatigue,
            ["satiety"] = data.m_satiety,
        };

        var history = new List<uint>();
        var historyDays = new List<object>(); // when (year/day) the partner reached which form
        try
        {
            var all = data.GetHistoryAll();
            if (all != null)
                for (int h = 0; h < all.Length; h++)
                    if (all[h].m_DigimonID != 0 && all[h].m_DigimonID != uint.MaxValue)
                    {
                        history.Add(all[h].m_DigimonID);
                        historyDays.Add(new Dictionary<string, object>
                        {
                            ["id"] = all[h].m_DigimonID, ["year"] = all[h].m_Year, ["day"] = all[h].m_Day,
                        });
                    }
        }
        catch (Exception e) { LogOnce("History", e); }

        PartnerCtrl pc = null;
        if (inField)
        {
            try { pc = MainGameManager.GetPartnerCtrl(idx); } catch { }
        }
        if (pc != null && _cmpTest == null) _cmpTest = CompareSelfTest(pc);

        var evos = new List<object>();
        if (param != null)
        {
            foreach (var evoId in NonZero(param.m_evolution1, param.m_evolution2, param.m_evolution3, param.m_evolution4, param.m_evolution5))
            {
                var e = ReadEvolution(evoId, values, pc);
                if (e != null) evos.Add(e);
            }
        }

        string nickname = null;
        try { nickname = c.m_name; } catch { }

        // The game's own decision: which form would the partner evolve into right now?
        object choice = null;
        if (pc != null)
        {
            try
            {
                var id = pc.CheckEvolution();
                var cp = id != 0 && id != uint.MaxValue ? ParameterDigimonData.GetParam(id) : null;
                choice = new Dictionary<string, object> { ["id"] = id, ["name"] = SafeName(cp) };
            }
            catch (Exception ex) { LogOnce("CheckEvolutionChoice", ex); }
        }

        return new Dictionary<string, object>
        {
            ["slot"] = idx,
            ["choice"] = choice,
            ["id"] = c.m_baseID,
            ["name"] = SafeName(param),
            ["nickname"] = nickname,
            ["growth"] = param?.m_growth ?? -1,
            ["age"] = data.m_age,
            ["lifetime"] = data.m_lifetime,
            ["timeFromAge"] = data.m_time_from_age,
            ["values"] = values,
            ["history"] = history,
            ["historyDays"] = historyDays,
            ["live"] = pc != null,
            ["evolutions"] = evos,
        };
    }

    static Dictionary<string, object> ReadEvolution(uint evoId, Dictionary<string, int> v, PartnerCtrl pc)
    {
        var t = ParameterDigimonData.GetParam(evoId);
        var name = SafeName(t);
        if (t == null || !IsValidName(name)) return null;

        var e = new Dictionary<string, object> { ["id"] = evoId, ["name"] = name };
        if (pc == null) return e;

        // The game's own priority and check (both already called every second without issues in plugin 1.0)
        try { e["points"] = pc.EvolutionPoint(evoId); } catch (Exception ex) { LogOnce("EvolutionPoint", ex); }
        try { e["ok"] = pc.CheckEvolution(evoId); } catch (Exception ex) { LogOnce("CheckEvolution", ex); }
        try { e["enable"] = pc.IsEnableEvolution(evoId); } catch (Exception ex) { LogOnce("IsEnableEvolution", ex); }
        try { e["growthPoint"] = pc.GetGrowthEvoPoint(evoId); } catch (Exception ex) { LogOnce("GetGrowthEvoPoint", ex); }

        // Use the game's comparison for each condition (1 = met)
        var cond = Conditions(t);
        var cmp = new Dictionary<string, int>();
        foreach (var key in cond.Keys)
        {
            try { cmp[key] = pc.ComparisonEvolution(v[key], cond[key]); }
            catch (Exception ex) { LogOnce("Comparison", ex); }
        }
        try { cmp["mistakes_curse"] = pc.ComparisonEvolution(v["curse"], cond["mistakes"]); } catch { }
        e["cmp"] = cmp;
        return e;
    }

    /// Check once how the game evaluates edge cases (condition 0).
    static Dictionary<string, object> CompareSelfTest(PartnerCtrl pc)
    {
        var r = new Dictionary<string, object>();
        foreach (var (val, cond) in new[] { (0, 0), (5, 0), (5, 3), (2, 3), (2, -3), (5, -3) })
        {
            try { r[$"{val},{cond}"] = pc.ComparisonEvolution(val, cond); }
            catch (Exception ex) { LogOnce("SelfTest", ex); }
        }
        return r;
    }

    static string SafeName(ParameterDigimonData p)
    {
        if (p == null) return null;
        try { return p.GetDefaultName(); } catch { return null; }
    }

    internal static void WriteAtomic(string file, object content)
    {
        Directory.CreateDirectory(OutDir);
        var path = Path.Combine(OutDir, file);
        var tmp = path + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(content, Json));
        File.Move(tmp, path, true);
    }
}
