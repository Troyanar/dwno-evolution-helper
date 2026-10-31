using System.Collections.Generic;
using System.Linq;

namespace DwnoEvoHelper;

/// Possible rules by which the game chooses when several targets are met.
/// Which one is right is checked against the evolution log.
public static class EvoRules
{
    /// Surplus rule (from Re:Digitize): most met conditions beyond the required number,
    /// then current Digimon as key, then the game's priority, then list order.
    public static EvoCard? BySurplus(IReadOnlyList<EvoCard> cards) =>
        cards.Select((c, i) => (c, i)).Where(x => x.c.Ready)
            .OrderByDescending(x => x.c.Met - x.c.Needed)
            .ThenByDescending(x => x.c.KeyIsCurrent ? 1 : 0)
            .ThenByDescending(x => x.c.Priority ?? int.MinValue)
            .ThenBy(x => x.i)
            .Select(x => x.c).FirstOrDefault();

    /// Highest game priority (EvolutionPoint) among the met targets
    public static EvoCard? ByPriority(IReadOnlyList<EvoCard> cards) =>
        cards.Select((c, i) => (c, i)).Where(x => x.c.Ready && x.c.Priority != null)
            .OrderByDescending(x => x.c.Priority).ThenBy(x => x.i)
            .Select(x => x.c).FirstOrDefault();

    /// Fallback when no target is fully met: the closest one (largest surplus, even if negative),
    /// then priority, then list order
    public static EvoCard? ClosestFallback(IReadOnlyList<EvoCard> cards) =>
        cards.Select((c, i) => (c, i)).Where(x => x.c.Needed > 0)
            .OrderByDescending(x => x.c.Met - x.c.Needed)
            .ThenByDescending(x => x.c.Priority ?? int.MinValue)
            .ThenBy(x => x.i)
            .Select(x => x.c).FirstOrDefault();

    /// First met target in the order of the game data
    public static EvoCard? ByListOrder(IReadOnlyList<EvoCard> cards) => cards.FirstOrDefault(c => c.Ready);

    /// Cards for all targets of a partner (conditions, priority, key = current Digimon)
    public static List<EvoCard> Cards(GameData data, LivePartner partner, bool useLiveCmp)
    {
        var me = data[partner.Id];
        var history = partner.HistorySet();
        var cards = new List<EvoCard>();
        if (me == null) return cards;
        foreach (var t in data.Targets(me))
        {
            var live = partner.Evolutions?.FirstOrDefault(e => e.Id == t.Id);
            var card = Evaluator.Build(data, t, partner.Value, history, useLiveCmp ? live?.Cmp : null, false);
            card.Priority = live?.Points;
            card.KeyIsCurrent = t.Keys.Contains(partner.Id);
            cards.Add(card);
        }
        return cards;
    }
}
