using System.Collections.Generic;
using System.Linq;

namespace DwnoEvoHelper;

/// Mögliche Regeln, nach denen das Spiel bei mehreren erfüllten Zielen wählt.
/// Welche stimmt, wird mit dem Digitations-Protokoll geprüft.
public static class EvoRules
{
    /// Überschuss-Regel (aus Re:Digitize): meiste erfüllte Bedingungen über dem Soll,
    /// dann aktuelles Digimon als Schlüssel, dann Priorität des Spiels, dann Listenreihenfolge.
    public static EvoCard? BySurplus(IReadOnlyList<EvoCard> cards) =>
        cards.Select((c, i) => (c, i)).Where(x => x.c.Ready)
            .OrderByDescending(x => x.c.Met - x.c.Needed)
            .ThenByDescending(x => x.c.KeyIsCurrent ? 1 : 0)
            .ThenByDescending(x => x.c.Priority ?? int.MinValue)
            .ThenBy(x => x.i)
            .Select(x => x.c).FirstOrDefault();

    /// Höchste Priorität des Spiels (EvolutionPoint) unter den erfüllten Zielen
    public static EvoCard? ByPriority(IReadOnlyList<EvoCard> cards) =>
        cards.Select((c, i) => (c, i)).Where(x => x.c.Ready && x.c.Priority != null)
            .OrderByDescending(x => x.c.Priority).ThenBy(x => x.i)
            .Select(x => x.c).FirstOrDefault();

    /// Ersatz, wenn kein Ziel voll erfüllt ist: am nächsten dran (größter Überschuss, auch negativ),
    /// dann Priorität, dann Listenreihenfolge
    public static EvoCard? ClosestFallback(IReadOnlyList<EvoCard> cards) =>
        cards.Select((c, i) => (c, i)).Where(x => x.c.Needed > 0)
            .OrderByDescending(x => x.c.Met - x.c.Needed)
            .ThenByDescending(x => x.c.Priority ?? int.MinValue)
            .ThenBy(x => x.i)
            .Select(x => x.c).FirstOrDefault();

    /// Erstes erfüllte Ziel in der Reihenfolge der Spieldaten
    public static EvoCard? ByListOrder(IReadOnlyList<EvoCard> cards) => cards.FirstOrDefault(c => c.Ready);

    /// Karten für alle Ziele eines Partners (Bedingungen, Priorität, Schlüssel = aktuelles Digimon)
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
