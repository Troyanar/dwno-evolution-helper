using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace DwnoEvoHelper;

/// Display of one partner (card, next evolutions, own target Digimon).
/// Persists per slot so the search box is not rebuilt during live updates.
public sealed class PartnerVm : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public PartnerVm(int slot) => Slot = slot;

    public int Slot { get; }

    T Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (!EqualityComparer<T>.Default.Equals(field, value))
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        return value;
    }

    Pic _avatar = new() { Name = "?" };
    public Pic Avatar { get => _avatar; set => Set(ref _avatar, value); }

    string _name = "—", _sub = "", _age = "", _life = "";
    public string Name { get => _name; set => Set(ref _name, value); }
    public string Sub { get => _sub; set => Set(ref _sub, value); }
    public string Age { get => _age; set => Set(ref _age, value); }
    public string Life { get => _life; set => Set(ref _life, value); }

    string _finding = "";
    public string Finding { get => _finding; set { Set(ref _finding, value); Notify(nameof(FindingVisibility)); } }
    public Visibility FindingVisibility => _finding == "" ? Visibility.Collapsed : Visibility.Visible;

    System.Windows.Media.Brush? _findingBrush;
    public System.Windows.Media.Brush? FindingBrush { get => _findingBrush; set => Set(ref _findingBrush, value); }

    string _timing = "";
    public string Timing { get => _timing; set { Set(ref _timing, value); Notify(nameof(TimingVisibility)); } }
    public Visibility TimingVisibility => _timing == "" ? Visibility.Collapsed : Visibility.Visible;

    string _plan = "";
    public string Plan { get => _plan; set { Set(ref _plan, value); Notify(nameof(PlanVisibility)); } }
    public Visibility PlanVisibility => _plan == "" ? Visibility.Collapsed : Visibility.Visible;

    string _warning = "";
    public string Warning { get => _warning; set { Set(ref _warning, value); Notify(nameof(WarningVisibility)); } }
    public Visibility WarningVisibility => _warning == "" ? Visibility.Collapsed : Visibility.Visible;

    public string TimingTooltip => Loc.T("timing.tooltip");

    List<Stat> _stats = new();
    public List<Stat> Stats { get => _stats; set => Set(ref _stats, value); }

    List<EvoCard> _cards = new();
    public List<EvoCard> Cards { get => _cards; set => Set(ref _cards, value); }

    string _emptyText = "";
    public string EmptyText { get => _emptyText; set { Set(ref _emptyText, value); Notify(nameof(EmptyVisibility)); } }
    public Visibility EmptyVisibility => _emptyText == "" ? Visibility.Collapsed : Visibility.Visible;

    string _nextTitle = "", _planTitle = "", _searchPlaceholder = "";
    public string NextTitle { get => _nextTitle; set => Set(ref _nextTitle, value); }
    public string PlanTitle { get => _planTitle; set => Set(ref _planTitle, value); }
    public string SearchPlaceholder { get => _searchPlaceholder; set => Set(ref _searchPlaceholder, value); }

    Thickness _columnMargin;
    public Thickness ColumnMargin { get => _columnMargin; set => Set(ref _columnMargin, value); }

    // ----- Target Digimon -----

    /// Selected target (Digimon name) or null
    public string? Goal { get; set; }

    string _query = "";
    public string Query
    {
        get => _query;
        set { Set(ref _query, value); Notify(nameof(PlaceholderVisibility)); }
    }
    public Visibility PlaceholderVisibility => _query.Length == 0 ? Visibility.Visible : Visibility.Collapsed;

    List<Pic> _suggestions = new();
    public List<Pic> Suggestions { get => _suggestions; set { Set(ref _suggestions, value); Notify(nameof(SuggestionsVisibility)); } }
    public Visibility SuggestionsVisibility => _suggestions.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

    List<Pic> _goalPath = new();
    public List<Pic> GoalPath { get => _goalPath; set => Set(ref _goalPath, value); }

    string _goalInfo = "";
    public string GoalInfo { get => _goalInfo; set { Set(ref _goalInfo, value); Notify(nameof(GoalInfoVisibility)); } }
    public Visibility GoalInfoVisibility => _goalInfo == "" ? Visibility.Collapsed : Visibility.Visible;

    List<EvoCard> _goalSteps = new();
    public List<EvoCard> GoalSteps { get => _goalSteps; set => Set(ref _goalSteps, value); }

    void Notify(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
