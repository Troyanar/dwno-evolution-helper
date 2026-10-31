using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace DwnoEvoHelper;

/// Partner view: one or both partners, each with its own target Digimon.
public partial class MainWindow
{
    const int BothPartners = 2;

    readonly Dictionary<int, PartnerVm> _vms = new();
    readonly ObservableCollection<PartnerVm> _panels = new();
    double _widthBeforeBoth;

    PartnerVm Vm(int slot)
    {
        if (!_vms.TryGetValue(slot, out var vm))
        {
            vm = new PartnerVm(slot);
            var goal = _settings.Goals != null && slot < _settings.Goals.Length ? _settings.Goals[slot] : null;
            vm.Goal = goal;
            vm.Query = goal ?? "";
            _vms[slot] = vm;
        }
        return vm;
    }

    // ---------- Texts ----------

    void ApplyTexts()
    {
        TitleText.Text = Loc.T("title");
        BuildLanguageChoices();
        Footer.Text = Loc.T("footer");
        TabBothText.Text = Loc.T("both");
        OpacityButton.ToolTip = Loc.T("tip.opacity");
        PinButton.ToolTip = Loc.T("tip.pin");
        SettingsButton.ToolTip = Loc.T("tip.settings");
        MinButton.ToolTip = Loc.T("tip.min");
        CloseButton.ToolTip = Loc.T("tip.close");
        SetupTitle.Text = Loc.T("setup.title");
        SetupIntro.Text = Loc.T("setup.intro");
        PickButton.Content = Loc.T("setup.pick");
        SearchButton.Content = Loc.T("setup.search");
        RemoveButton.Content = Loc.T("setup.remove");
        SetupCloseButton.Content = Loc.T("setup.close") + " →";
        ClickThroughHint.Text = Loc.T("hk.clickthrough");
        HotkeyInfo.Text = Loc.T("hk.info");
        TitleText.ToolTip = Loc.T("hk.info");
        ApplyDexTexts();
        _lastRender = "";
        if (SetupView.Visibility == Visibility.Visible) RenderSetup();
    }

    // ---------- Main view ----------

    void Render()
    {
        bool running = GameRunning();
        var partners = _live?.Partners ?? new List<LivePartner>();
        bool hasPartners = partners.Count > 0;

        SetStatus(NeedsSetup() && _data == null ? ("st.setup", "Bad")
                : !running ? (hasPartners ? "st.off" : "st.wait", "Muted")
                : !GameFresh() ? ("st.paused", "Accent")
                : !(_live?.InGame ?? false) ? ("st.menu", "Accent")
                : ("st.live", "Good"));

        // Only redraw when something has changed (otherwise tooltips flicker)
        var key = string.Join("|", partners.Select(p => p.Id + ":" + string.Join(",", p.Values.Select(v => v.Value)) +
                                                        ":" + string.Join(",", p.Evolutions.Select(e => e.Cmp == null ? "" : string.Join("", e.Cmp.Values)))))
                  + _partner + string.Join(",", _vms.Values.Select(v => v.Goal)) + _dataStamp + running + GameFresh() + Loc.Lang
                  + (_live?.World is WorldInfo w ? $"{w.TotalDay}:{w.Hour}:{w.Minute / 10}" : "") + Timing.HasRate // keep the time line updating
                  + Trainer.SessionCount;
        if (key == _lastRender) return;
        _lastRender = key;
        RenderDexDetail();

        // Tabs: name + image of the partners
        LivePartner? P(int slot) => partners.FirstOrDefault(p => p.Slot == slot);
        Pic TabPic(LivePartner? p) => p != null && _data?[p.Id] is GameDigimon d ? Pic.Of(d) : new Pic { Name = Nm(p?.Name) };
        Tab0Text.Text = P(0) is LivePartner p0 ? Nm(p0.Name) : Loc.F("partner", 1);
        Tab1Text.Text = P(1) is LivePartner p1 ? Nm(p1.Name) : Loc.F("partner", 2);
        Tab0Pic.Content = TabPic(P(0));
        Tab1Pic.Content = TabPic(P(1));

        // Which partners are shown?
        var slots = _partner == BothPartners ? new[] { 0, 1 } : new[] { _partner };
        var shown = slots.Select(Vm).ToList();
        for (int i = 0; i < shown.Count; i++)
            shown[i].ColumnMargin = shown.Count == 1 ? new Thickness(0) : i == 0 ? new Thickness(0, 0, 6, 0) : new Thickness(6, 0, 0, 0);
        if (!_panels.SequenceEqual(shown))
        {
            _panels.Clear();
            foreach (var vm in shown) _panels.Add(vm);
        }

        foreach (var vm in shown)
        {
            var partner = P(vm.Slot) ?? (_partner != BothPartners ? partners.FirstOrDefault() : null);
            Fill(vm, partner, running);
        }
    }

    void Fill(PartnerVm vm, LivePartner? partner, bool running)
    {
        vm.NextTitle = Loc.T("sec.next");
        vm.PlanTitle = Loc.T("sec.plan");
        vm.SearchPlaceholder = Loc.T("search");

        var me = partner != null && _data != null ? _data[partner.Id] : null;
        if (partner == null || _data == null)
        {
            vm.Name = "—";
            vm.Avatar = new Pic { Name = "?" };
            vm.Sub = Loc.T(_data == null ? "noData" : "noPartner");
            vm.Age = vm.Life = "";
            vm.Stats = new List<Stat>();
            vm.Cards = new List<EvoCard>();
            vm.EmptyText = Loc.T("evo.nodata");
            vm.Finding = "";
            vm.Timing = "";
            vm.Plan = vm.Warning = "";
            RenderGoal(vm, null, null);
            return;
        }

        vm.Name = Nm(partner.Name);
        vm.Avatar = me != null ? Pic.Of(me) : new Pic { Name = Nm(partner.Name) };
        // Stage line: stage · age · lifetime (· snapshot time, if no live data)
        var parts = new List<string> { Evaluator.Stage(partner.Growth), Loc.F("age", partner.Age) };
        if (Timing.LifeDays(partner, _live!.World) is double ld) parts.Add(Loc.F("life", ld.ToString("0.0", Loc.Culture)));
        if (!(running && GameFresh())) parts.Add(Loc.F("asof", _live.Time.ToString("t", Loc.Culture)));
        if (partner.Nickname != null && partner.Nickname != partner.Name) parts.Add($"\"{partner.Nickname}\"");
        vm.Sub = string.Join(" · ", parts);

        string N(string k) => partner.Value(k).ToString("N0", Loc.Culture);
        vm.Stats = new[] { "HP", "MP", "STR", "STA", "WIS", "SPD", "weight", "mistakes", "bond", "discipline", "wins" }
            .Select(k => new Stat { Label = Evaluator.Label(k), Value = N(k.ToLowerInvariant()) }).ToList();

        // Possible evolutions in game priority
        // Cards for all targets (conditions, game priority, key = current form)
        var cards = EvoRules.Cards(_data, partner, useLiveCmp: running);
        foreach (var card in cards.Where(c => c.Priority != null))
        {
            card.PriorityText = Loc.F("prio.short", card.Priority!.Value.ToString("N0", Loc.Culture));
            card.PointsTooltip = Loc.F("priority", card.Priority);
        }
        // per-card training hints only once there are measured sessions (before that the header says "train once")
        foreach (var card in cards.Where(c => !c.Ready && _data[c.Id] != null && Trainer.SessionCount > 0))
        {
            var train = Trainer.Describe(_data[card.Id]!, partner.Value);
            if (train != "") card.Hint += (card.Hint != "" ? "\n" : "") + train;
        }
        vm.Cards = cards;

        // The one finding, right below the name:
        // the game's decision (live), plus the prediction by the surplus rule
        var ready = cards.FirstOrDefault(c => c.Ready);
        var closest = cards.Where(c => c.Needed > 0).OrderByDescending(c => (double)c.Met / c.Needed).FirstOrDefault();
        var predicted = EvoRules.BySurplus(cards);
        var choice = running && GameFresh() ? partner.Choice : null;
        var choiceCard = choice != null && choice.Valid ? cards.FirstOrDefault(c => c.Id == choice.Id) : null;
        if (predicted != null && predicted != choiceCard)
        {
            predicted.Badge = Loc.T("badge.predicted");
        }
        if (choiceCard != null)
        {
            choiceCard.Badge = Loc.T("badge.choice");
            choiceCard.Border = (Brush)FindResource("PositiveLine");
        }

        string Prediction(EvoCard c) =>
            Loc.F("finding.predicted", c.Name, (c.Met - c.Needed).ToString("+0;-0;+0"))
            + (c.Fragile != "" ? " – " + Loc.F("finding.fragile", c.Fragile) : "");
        string Closest() => closest == null ? "" :
            Loc.F("finding.closest", closest.Name, closest.Met, closest.Needed)
            + (closest.Missing != "" ? " – " + Loc.F("finding.missing", closest.Missing) : "");

        vm.Finding =
            choice != null && choice.Valid
                ? Loc.F("finding.choice", Nm(choice.Name))
                  + (predicted == null ? "" : predicted.Id == choice.Id ? " · " + Loc.T("finding.agree") : " · " + Prediction(predicted))
            : choice != null
                ? Loc.T("finding.nochoice") + " · " + (predicted != null ? Prediction(predicted) : Closest())
            : predicted != null ? Prediction(predicted)
            : Closest();
        vm.FindingBrush = (Brush)FindResource(choice?.Valid == true || (choice == null && predicted != null) ? "Good" : "NeutralInk");
        // Training plan for the target it is currently heading towards
        var focus = predicted ?? closest;
        var focusTarget = focus != null ? _data[focus.Id] : null;
        var planText = focus != null && !focus.Ready && focusTarget != null ? Trainer.Describe(focusTarget, partner.Value) : "";
        vm.Plan = planText == "" ? "" : Loc.F("train.for", focus!.Name, planText)
                  + (Trainer.SessionCount == 0 ? "" : " " + Loc.F("train.measured", Trainer.SessionCount));
        if (focus != null && !focus.Ready && Trainer.SessionCount == 0 && Trainer.Need(focusTarget!, partner.Value).Any(n => n > 0))
            vm.Plan = Loc.T("train.first");

        // Warnings: lifetime, age of the partners
        var warnings = new List<string>();
        var life = Timing.LifeDays(partner, _live!.World);
        var untilEvo = Timing.DaysUntilWindow(partner);
        if (life is double ld2 && ld2 < 1) warnings.Add(Loc.T("warn.dying"));
        else if (life is double ld3 && untilEvo is int ue && ld3 < ue) warnings.Add(Loc.F("warn.life", ld3.ToString("0.0", Loc.Culture), ue));
        var other = (_live!.Partners ?? new List<LivePartner>()).FirstOrDefault(o => o.Slot != partner.Slot);
        if (other != null && Math.Abs(other.Age - partner.Age) >= 2) warnings.Add(Loc.F("warn.agegap", Math.Abs(other.Age - partner.Age), Nm(other.Name)));
        vm.Warning = string.Join(" · ", warnings);

        // When does it evolve? (days until the age window, time until the next day change)
        vm.Timing = me != null && _data.Targets(me).Any() ? Timing.Describe(partner, _live!.World, ready != null) : Loc.T("timing.final");
        vm.EmptyText = cards.Count == 0 ? Loc.T(me != null ? "evo.final" : "evo.nodata") : "";

        RenderGoal(vm, me, partner);
    }

    /// Digimon name from the plugin (game language) → name for the current UI language
    string Nm(string? raw) => Loc.DigimonName(raw ?? "?", _data?.Lang);

    /// Language buttons in the setup screen (English is the default, "Auto" follows the game)
    void BuildLanguageChoices()
    {
        LangTitle.Text = Loc.T("lang.title") + "  ·  " + Loc.T("lang.auto");
        LangChoices.Children.Clear();
        foreach (var (code, name) in Loc.Choices)
        {
            var rb = new RadioButton
            {
                Content = name,
                GroupName = "lang",
                Style = (Style)FindResource("FilterToggle"),
                IsChecked = Loc.Choice == code,
            };
            rb.Checked += (_, _) =>
            {
                if (Loc.Choice == code) return;
                Loc.Choice = _settings.Language = code;
                // rebuild after the click has been handled
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    Loc.Set(_live?.Lang ?? _data?.Lang);
                    _data?.ApplyNames();
                    ApplyTexts();
                }));
            };
            LangChoices.Children.Add(rb);
        }
    }

    void SetStatus((string Text, string Brush) s)
    {
        StatusText.Text = Loc.T(s.Text);
        StatusDot.Fill = (Brush)FindResource(s.Brush);
        StatusText.Foreground = (Brush)FindResource(s.Brush == "Muted" ? "Muted" : "Text");
    }

    void Tab_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is not RadioButton rb || !int.TryParse(rb.Tag?.ToString(), out var i)) return;
        var wasBoth = _partner == BothPartners;
        _partner = i;

        // Widen the window for both partners side by side – and shrink it back afterwards
        if (i == BothPartners && !wasBoth && Width < 820)
        {
            _widthBeforeBoth = Width;
            Width = 880;
        }
        else if (i != BothPartners && wasBoth && _widthBeforeBoth > 0)
        {
            Width = _widthBeforeBoth;
            _widthBeforeBoth = 0;
        }

        _lastRender = "";
        if (IsLoaded) Render();
    }

    // ---------- Target Digimon (per partner) ----------

    void RenderGoal(PartnerVm vm, GameDigimon? me, LivePartner? partner)
    {
        vm.GoalPath = new List<Pic>();
        vm.GoalSteps = new List<EvoCard>();
        vm.GoalInfo = "";
        if (vm.Goal == null || _data == null) return;

        var goal = _data.Selectable().FirstOrDefault(d => d.Name == vm.Goal);
        if (goal == null) return;

        Func<string, int> value = partner != null ? partner.Value : _ => 0;
        var history = partner?.HistorySet() ?? new HashSet<uint>();

        var path = me != null ? _data.PathTo(me, vm.Goal!) : null;
        if (path != null && path.Count > 1)
        {
            vm.GoalPath = path.Select((p, i) =>
            {
                var pic = Pic.Of(p);
                pic.ArrowVisibility = i < path.Count - 1 ? Visibility.Visible : Visibility.Collapsed;
                return pic;
            }).ToList();
            // The conditions for each stage – checked against today's stats
            vm.GoalSteps = path.Skip(1)
                .Select((step, i) => Evaluator.Build(_data, step, value,
                    new HashSet<uint>(history.Concat(path.Take(i + 1).Select(p => p.Id))), null, false))
                .ToList();
            return;
        }

        if (path != null)
        {
            vm.GoalInfo = Loc.F("plan.already", goal.Name);
            return;
        }

        var pre = _data.Predecessors(goal.Name).Select(p => p.Name).ToList();
        vm.GoalInfo = Loc.F("plan.unreach", me?.Name ?? Loc.T("your.partner")) + "\n" +
                      (pre.Count > 0 ? Loc.F("plan.from", string.Join(", ", pre)) : Loc.T("plan.special"));
        vm.GoalSteps = new List<EvoCard> { Evaluator.Build(_data, goal, partner != null ? value : null, history, null, false) };
    }

    static PartnerVm? VmOf(object sender) => (sender as FrameworkElement)?.DataContext as PartnerVm;

    void PartnerGoal_TextChanged(object sender, TextChangedEventArgs e)
    {
        var vm = VmOf(sender);
        if (vm == null) return;
        var q = ((TextBox)sender).Text.Trim();
        if (q.Length == 0 || _data == null || q == vm.Goal)
        {
            vm.Suggestions = new List<Pic>();
            if (q.Length == 0 && vm.Goal != null) SetGoal(vm, null);
            return;
        }
        vm.Suggestions = _data.Selectable()
            .Where(d => d.Name.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
            .OrderBy(d => d.Name.StartsWith(q, StringComparison.OrdinalIgnoreCase) ? 0 : 1).ThenBy(d => d.Name)
            .Take(12).Select(Pic.Of).ToList();
    }

    /// The suggestion list sits in the same StackPanel directly below the search box.
    static ListBox? SuggestionList(TextBox box) =>
        ((box.Parent as FrameworkElement)?.Parent as Panel)?.Children.OfType<ListBox>().FirstOrDefault();

    void PartnerGoal_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        var vm = VmOf(sender);
        if (vm == null || vm.Suggestions.Count == 0) return;
        if (e.Key == Key.Down && SuggestionList((TextBox)sender) is ListBox list)
        {
            list.SelectedIndex = 0;
            (list.ItemContainerGenerator.ContainerFromIndex(0) as ListBoxItem)?.Focus();
            e.Handled = true;
        }
        else if (e.Key == Key.Enter)
        {
            PickGoal(vm, vm.Suggestions[0].Name);
            e.Handled = true;
        }
    }

    void PartnerGoalSuggestion_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (VmOf(sender) is PartnerVm vm && (sender as ListBox)?.SelectedItem is Pic p) PickGoal(vm, p.Name);
    }

    void PartnerGoalSuggestion_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && VmOf(sender) is PartnerVm vm && (sender as ListBox)?.SelectedItem is Pic p) PickGoal(vm, p.Name);
    }

    void PickGoal(PartnerVm vm, string name)
    {
        vm.Goal = name;
        vm.Query = name;
        vm.Suggestions = new List<Pic>();
        SetGoal(vm, name);
    }

    void SetGoal(PartnerVm vm, string? name)
    {
        vm.Goal = name;
        var goals = _settings.Goals ?? new string?[2];
        if (goals.Length < 2) Array.Resize(ref goals, 2);
        goals[vm.Slot] = name;
        _settings.Goals = goals;
        _lastRender = "";
        Render();
    }
}
