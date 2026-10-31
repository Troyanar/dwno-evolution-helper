using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace DwnoEvoHelper;

// Files written by the BepInEx plugin (%LOCALAPPDATA%\DWNO-EvoHelper)

[DataContract]
public sealed class GameDigimon
{
    [DataMember(Name = "id")] public uint Id { get; set; }
    [DataMember(Name = "name")] public string Name { get; set; } = "";
    [IgnoreDataMember] public string? OriginalName { get; set; }   // name as delivered by the game
    [DataMember(Name = "icon")] public string? Icon { get; set; }
    [DataMember(Name = "growth")] public int Growth { get; set; }
    [DataMember(Name = "evo")] public Dictionary<string, int> Evo { get; set; } = new();
    [DataMember(Name = "keys")] public List<uint> Keys { get; set; } = new();
    [DataMember(Name = "matches")] public int Matches { get; set; }
    [DataMember(Name = "to")] public List<uint> To { get; set; } = new();
    [DataMember(Name = "from")] public List<uint> From { get; set; } = new();
    [DataMember(Name = "flags")] public int Flags { get; set; }
}

[DataContract]
public sealed class GameData
{
    [DataMember(Name = "lang")] public string? Lang { get; set; }
    [DataMember(Name = "plugin")] public string? Plugin { get; set; }
    [DataMember(Name = "digimon")] public List<GameDigimon> Digimon { get; set; } = new();

    Dictionary<uint, GameDigimon> _byId = new();
    public GameDigimon? this[uint id] => _byId.TryGetValue(id, out var d) ? d : null;

    public static GameData? TryLoad(string path) => Index(Json.Read<GameData>(path));

    /// Table shipped in the EXE (German names) – for Digidex/planning without starting the game.
    /// As soon as the plugin writes its own table, that one is used (names in the game language).
    public static GameData? LoadEmbedded()
    {
        using var s = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("gamedata.json");
        return s == null ? null : Index(Json.Read<GameData>(s));
    }

    static GameData? Index(GameData? data)
    {
        if (data == null || data.Digimon == null || data.Digimon.Count == 0) return null;
        data._byId = data.Digimon.GroupBy(d => d.Id).ToDictionary(g => g.Key, g => g.First());
        data.ApplyNames();
        return data;
    }

    /// Re-applies display names for the current UI language (call after a language change)
    public void ApplyNames()
    {
        foreach (var d in Digimon)
        {
            d.OriginalName ??= d.Name;
            d.Name = Loc.DigimonName(d.OriginalName, Lang);
        }
    }

    /// All Digimon for the Digidex: one entry per name. Hidden are NPC copies (flag 2)
    /// and the humans the game keeps in the same table (icon code "z…").
    /// The normal entry (flag 0) with real evolution conditions is preferred.
    public IEnumerable<GameDigimon> Dex() =>
        Digimon.Where(d => IsValidName(d.Name) && d.Flags != 2 && !(d.Icon ?? "").StartsWith("z"))
            .GroupBy(d => d.Name)
            .Select(g => g.OrderBy(d => d.Flags == 0 ? 0 : 1).ThenBy(d => d.Matches == Evaluator.NoCondition ? 1 : 0).First())
            .OrderBy(d => d.Growth).ThenBy(d => d.Name);

    /// Placeholder entries are named "ランゲージが見つかりません！！" in the game (language not found).
    public static bool IsValidName(string? name) =>
        !string.IsNullOrWhiteSpace(name) && name!.IndexOf("ランゲージ", StringComparison.Ordinal) < 0 &&
        name.IndexOf("見つかりません", StringComparison.Ordinal) < 0;

    /// Real Digimon with their own evolution conditions (no NPC copies), only once per name.
    public IEnumerable<GameDigimon> Selectable() =>
        Digimon.Where(d => IsValidName(d.Name) && d.Flags == 0 && d.Matches != Evaluator.NoCondition)
            .GroupBy(d => d.Name).Select(g => g.First()).OrderBy(d => d.Name);

    /// Valid evolution targets of a Digimon (in priority order).
    public IEnumerable<GameDigimon> Targets(GameDigimon d) =>
        d.To.Select(id => this[id]).Where(t => t != null && IsValidName(t.Name)).Select(t => t!);

    /// Shortest evolution path (via Digimon IDs).
    public List<GameDigimon>? PathTo(GameDigimon start, string goalName)
    {
        var prev = new Dictionary<uint, uint> { [start.Id] = 0 };
        var queue = new Queue<GameDigimon>();
        queue.Enqueue(start);
        while (queue.Count > 0)
        {
            var cur = queue.Dequeue();
            if (string.Equals(cur.Name, goalName, StringComparison.OrdinalIgnoreCase))
            {
                var path = new List<GameDigimon>();
                for (uint id = cur.Id; id != 0; id = prev[id]) path.Add(this[id]!);
                path.Reverse();
                return path;
            }
            foreach (var t in Targets(cur))
            {
                if (prev.ContainsKey(t.Id)) continue;
                prev[t.Id] = cur.Id;
                queue.Enqueue(t);
            }
        }
        return null;
    }

    /// Digimon that can evolve directly into goalName.
    public IEnumerable<GameDigimon> Predecessors(string goalName) =>
        Digimon.Where(d => IsValidName(d.Name) && Targets(d).Any(t => string.Equals(t.Name, goalName, StringComparison.OrdinalIgnoreCase)))
            .GroupBy(d => d.Name).Select(g => g.First());
}

[DataContract]
public sealed class LiveEvolution
{
    [DataMember(Name = "id")] public uint Id { get; set; }
    [DataMember(Name = "cmp")] public Dictionary<string, int>? Cmp { get; set; }
    [DataMember(Name = "points")] public int? Points { get; set; }   // priority according to the game (since plugin 1.3)
}

/// The game's decision (PartnerCtrl.CheckEvolution(), since plugin 1.4)
[DataContract]
public sealed class ChoiceInfo
{
    [DataMember(Name = "id")] public uint Id { get; set; }
    [DataMember(Name = "name")] public string? Name { get; set; }
    public bool Valid => Id != 0 && Id != uint.MaxValue && GameData.IsValidName(Name);
}

[DataContract]
public sealed class LivePartner
{
    [DataMember(Name = "slot")] public int Slot { get; set; }
    [DataMember(Name = "id")] public uint Id { get; set; }
    [DataMember(Name = "name")] public string? Name { get; set; }
    [DataMember(Name = "nickname")] public string? Nickname { get; set; }
    [DataMember(Name = "growth")] public int Growth { get; set; }
    [DataMember(Name = "age")] public int Age { get; set; }
    [DataMember(Name = "lifetime")] public float Lifetime { get; set; }        // game minutes
    [DataMember(Name = "timeFromAge")] public float TimeFromAge { get; set; }  // game minutes since the last age increase
    [DataMember(Name = "values")] public Dictionary<string, int> Values { get; set; } = new();
    [DataMember(Name = "history")] public List<uint> History { get; set; } = new();
    [DataMember(Name = "live")] public bool Live { get; set; }
    [DataMember(Name = "evolutions")] public List<LiveEvolution> Evolutions { get; set; } = new();
    [DataMember(Name = "choice")] public ChoiceInfo? Choice { get; set; }

    public int Value(string key)
    {
        // Plugin 1.0 did not know "mistakes" yet
        if (Values.TryGetValue(key, out var v)) return v;
        if (key == "mistakes" && Values.TryGetValue("trainingFailure", out v)) return v;
        return 0;
    }

    public HashSet<uint> HistorySet() => new(History.Where(h => h != uint.MaxValue));
}

[DataContract]
public sealed class LiveState
{
    [DataMember(Name = "time")] public string? TimeText { get; set; }
    [DataMember(Name = "pid")] public int Pid { get; set; }
    [DataMember(Name = "lang")] public string? Lang { get; set; }
    [DataMember(Name = "plugin")] public string? Plugin { get; set; }
    [DataMember(Name = "inGame")] public bool InGame { get; set; }
    [DataMember(Name = "icons")] public string? Icons { get; set; }
    [DataMember(Name = "partners")] public List<LivePartner> Partners { get; set; } = new();
    [DataMember(Name = "world")] public WorldInfo? World { get; set; }

    public DateTime Time =>
        DateTime.TryParse(TimeText, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var t) ? t.ToLocalTime() : DateTime.MinValue;

    public static LiveState? TryLoad(string path)
    {
        var s = Json.Read<LiveState>(path);
        if (s != null) s.Partners ??= new();
        return s;
    }
}

/// Game clock (since plugin 1.2)
[DataContract]
public sealed class WorldInfo
{
    [DataMember(Name = "hour")] public int Hour { get; set; }
    [DataMember(Name = "minute")] public int Minute { get; set; }
    [DataMember(Name = "totalDay")] public uint TotalDay { get; set; }
    [DataMember(Name = "secPerHour")] public float SecPerHour { get; set; }   // real seconds per game hour
    [DataMember(Name = "hoursPerDay")] public float HoursPerDay { get; set; }
}

public static class GameFiles
{
    public static readonly string Dir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DWNO-EvoHelper");

    public static string LivePath => Path.Combine(Dir, "live.json");
    public static string GameDataPath => Path.Combine(Dir, "gamedata.json");
    public static string IconDir => Path.Combine(Dir, "icons");
}

public static class Json
{
    static DataContractJsonSerializerSettings Settings => new() { UseSimpleDictionaryFormat = true };

    public static T? Read<T>(string path) where T : class
    {
        try
        {
            if (!File.Exists(path)) return null;
            // file may be in the middle of being replaced by the plugin → allow sharing
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            return Read<T>(fs);
        }
        catch
        {
            return null;
        }
    }

    public static T? Read<T>(Stream s) where T : class
    {
        try { return (T?)new DataContractJsonSerializer(typeof(T), Settings).ReadObject(s); }
        catch { return null; }
    }

    public static void Write<T>(string path, T value)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var ms = new MemoryStream();
        new DataContractJsonSerializer(typeof(T), Settings).WriteObject(ms, value);
        File.WriteAllText(path, Encoding.UTF8.GetString(ms.ToArray()));
    }
}
