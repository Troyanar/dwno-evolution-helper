using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace DwnoEvoHelper;

/// Digidex: browse all Digimon with their evolution conditions and plan paths.
public partial class MainWindow
{
    bool _dexMode;
    int? _dexStage;            // null = all stages
    string _dexQuery = "";
    uint? _dexSelected;
    readonly Stack<uint> _dexHistory = new();

    void ApplyDexTexts()
    {
        ModePartnerText.Text = Loc.T("mode.partner");
        ModeDexText.Text = Loc.T("mode.dex");
        ModeLogText.Text = Loc.T("mode.log");
        LogReportButton.Content = Loc.T("log.report");
        DexPlaceholder.Text = Loc.T("dex.search");
        DexBack.Content = "← " + Loc.T("dex.back");
        DexFromTitle.Text = Loc.T("dex.from");
        DexToTitle.Text = Loc.T("dex.to");
        BuildStageFilters();
        RenderDexList();
        RenderDexDetail();
    }

    /// Show the partner, Digidex or setup view
    void ApplyMode()
    {
        bool setup = SetupView.Visibility == Visibility.Visible;
        MainArea.Visibility = !setup && !_dexMode && !_logMode ? Visibility.Visible : Visibility.Collapsed;
        LogArea.Visibility = !setup && _logMode ? Visibility.Visible : Visibility.Collapsed;
        DexArea.Visibility = !setup && _dexMode ? Visibility.Visible : Visibility.Collapsed;
        ModeBar.Visibility = setup ? Visibility.Collapsed : Visibility.Visible;
        Footer.Visibility = MainArea.Visibility;
    }

    void Mode_Checked(object sender, RoutedEventArgs e)
    {
        _dexMode = sender == ModeDex;
        _logMode = sender == ModeLog;
        if (!IsLoaded) return;
        ApplyMode();
        if (_logMode) RenderLog(force: true);
        if (_dexMode)
        {
            RenderDexList();
            RenderDexDetail();
        }
    }

    void BuildStageFilters()
    {
        DexStages.Children.Clear();
        var stages = _data?.Dex().Select(d => d.Growth).Where(g => g >= 0 && g <= 6).Distinct().OrderBy(g => g).ToList() ?? new List<int>();
        void Add(string text, int? stage)
        {
            var rb = new RadioButton
            {
                Content = text,
                GroupName = "dexstage",
                Style = (Style)FindResource("FilterToggle"),
                IsChecked = _dexStage == stage,
            };
            rb.Checked += (_, _) =>
            {
                _dexStage = stage;
                RenderDexList();
            };
            DexStages.Children.Add(rb);
        }
        Add(Loc.T("dex.all"), null);
        foreach (var g in stages) Add(Evaluator.Stage(g), g);
    }

    void RenderDexList()
    {
        if (_data == null)
        {
            DexList.ItemsSource = null;
            DexCount.Text = "";
            return;
        }
        var rows = _data.Dex()
            .Where(d => _dexStage == null || d.Growth == _dexStage)
            .Where(d => _dexQuery.Length == 0 || d.Name.IndexOf(_dexQuery, StringComparison.OrdinalIgnoreCase) >= 0)
            .Select(d => new DexRow
            {
                Id = d.Id,
                Name = d.Name,
                Image = Icons.Get(d.Icon),
                Stage = Evaluator.Stage(d.Growth),
                Summary = Evaluator.Summary(_data, d),
            })
            .ToList();
        DexList.ItemsSource = rows;
        DexCount.Text = Loc.F("dex.count", rows.Count);
    }

    void DexSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        _dexQuery = DexSearch.Text.Trim();
        DexPlaceholder.Visibility = _dexQuery.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        RenderDexList();
    }

    void DexItem_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not Pic p || p.Id == 0) return;
        if (_dexSelected is uint cur && cur != p.Id) _dexHistory.Push(cur);
        _dexSelected = p.Id;
        if (!_dexMode) ModeDex.IsChecked = true;
        RenderDexDetail();
        DexView.ScrollToTop();
    }

    void DexBack_Click(object sender, RoutedEventArgs e)
    {
        _dexSelected = _dexHistory.Count > 0 ? _dexHistory.Pop() : null;
        RenderDexDetail();
        DexView.ScrollToTop();
    }

    void DexClose_Click(object sender, RoutedEventArgs e)
    {
        _dexSelected = null;
        _dexHistory.Clear();
        RenderDexDetail();
    }

    void DexCompare_Changed(object sender, RoutedEventArgs e) => RenderDexDetail();

    LivePartner? CurrentPartner()
    {
        var partners = _live?.Partners ?? new List<LivePartner>();
        return partners.FirstOrDefault(p => p.Slot == _partner) ?? partners.FirstOrDefault();
    }

    void RenderDexDetail()
    {
        var d = _dexSelected is uint id && _data != null ? _data[id] : null;
        if (d == null || _data == null)
        {
            DexDetail.Visibility = Visibility.Collapsed;
            return;
        }
        DexDetail.Visibility = Visibility.Visible;
        DexBack.Visibility = _dexHistory.Count > 0 ? Visibility.Visible : Visibility.Hidden;
        DexPic.Content = Pic.Of(d);
        DexName.Text = d.Name;
        DexStage.Text = Evaluator.Stage(d.Growth);

        // Comparison with the selected partner (if game data is available)
        var partner = CurrentPartner();
        bool hasReq = d.Matches != Evaluator.NoCondition;
        DexCompare.Visibility = partner != null && hasReq ? Visibility.Visible : Visibility.Collapsed;
        if (partner != null)
        {
            DexCompare.Content = Loc.F("dex.compare", Nm(partner.Name));
            if (DexCompare.IsChecked == null) DexCompare.IsChecked = true;
        }
        bool compare = partner != null && hasReq && DexCompare.IsChecked == true;

        DexReqTitle.Text = Loc.F("dex.req", d.Name);
        DexReq.ItemsSource = new[]
        {
            compare ? Evaluator.Build(_data, d, partner!.Value, partner.HistorySet(), null, false) : Evaluator.Requirements(_data, d),
        };
        DexReq.ItemsSource.OfType<EvoCard>().ToList().ForEach(c => c.HeaderVisibility = Visibility.Collapsed); // name/image are already shown above

        var from = _data.Predecessors(d.Name).Select(Pic.Of).ToList();
        DexFrom.ItemsSource = from;
        DexFromEmpty.Text = from.Count == 0 ? Loc.T(d.Growth == 0 ? "dex.start" : "plan.special") : "";
        DexFromEmpty.Visibility = from.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        var to = _data.Targets(d).GroupBy(t => t.Name).Select(g => Pic.Of(g.First())).ToList();
        DexTo.ItemsSource = to;
        DexToEmpty.Text = Loc.T("dex.final");
        DexToEmpty.Visibility = to.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        // Path from the current partner
        var me = partner != null ? _data[partner.Id] : null;
        bool showPath = me != null;
        DexPathTitle.Visibility = DexPathInfo.Visibility = DexPath.Visibility = showPath ? Visibility.Visible : Visibility.Collapsed;
        if (!showPath) return;

        DexPathTitle.Text = Loc.F("dex.path", Nm(partner!.Name));
        var path = _data.PathTo(me!, d.Name);
        if (path != null && path.Count > 1)
        {
            DexPathInfo.Visibility = Visibility.Collapsed;
            DexPath.ItemsSource = path.Select((p, i) =>
            {
                var pic = Pic.Of(p);
                pic.ArrowVisibility = i < path.Count - 1 ? Visibility.Visible : Visibility.Collapsed;
                return pic;
            }).ToList();
        }
        else
        {
            DexPath.ItemsSource = null;
            DexPathInfo.Text = path != null ? Loc.F("plan.already", d.Name) : Loc.F("plan.unreach", me!.Name);
        }
    }
}
