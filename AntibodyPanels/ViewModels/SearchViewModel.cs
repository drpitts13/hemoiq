using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using AntibodyPanels.Data;
using AntibodyPanels.Models;

namespace AntibodyPanels.ViewModels
{
    public class SearchViewModel : BaseViewModel
    {
        private readonly DatabaseService _db;
        private readonly MainViewModel _main;

        public ObservableCollection<AntigenCriterionRow> Criteria { get; } = new();
        public ObservableCollection<SearchResultRow> Results { get; } = new();
        public IReadOnlyList<string> ZygosityOptions { get; } = AntigenConstants.PositiveZygosityOptions;

        public ICommand SearchCommand { get; }
        public ICommand OpenCommand { get; }
        public ICommand ClearCommand { get; }

        private string? _completedStatus;

        private SearchResultRow? _selectedResult;
        public SearchResultRow? SelectedResult
        {
            get => _selectedResult;
            set
            {
                var previous = ResultKey(_selectedResult);
                if (!SetField(ref _selectedResult, value)) return;
                if (_main.SelectedTabIndex == 7 && previous != ResultKey(_selectedResult))
                    AnnounceStatus();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private string _selectedZygosity = AntigenConstants.ZygosityBoth;
        public string SelectedZygosity
        {
            get => _selectedZygosity;
            set => SetField(ref _selectedZygosity, value);
        }

        public SearchViewModel(DatabaseService db, MainViewModel main)
        {
            _db = db;
            _main = main;

            foreach (var ag in AntigenConstants.AllKnownAntigens)
                Criteria.Add(new AntigenCriterionRow(ag));

            SearchCommand = new RelayCommand(RunSearch);
            OpenCommand = new RelayCommand(OpenSelected, () => SelectedResult != null);
            ClearCommand = new RelayCommand(ClearCriteria);
        }

        public void OpenResult(SearchResultRow row) =>
            _main.OpenPanel(row.PanelId, row.CellNumber, LastActiveAntigen(Criteria), ActiveAntigens(Criteria));

        /// <summary>
        /// The first antigen the search is actually filtering on, in the
        /// order the criteria are shown. Any is ignored.
        /// </summary>
        public static string? FirstActiveAntigen(IEnumerable<AntigenCriterionRow> criteria)
        {
            foreach (var criterion in criteria)
            {
                if (criterion.Selected is "+" or "-")
                    return criterion.Antigen;
            }
            return null;
        }

        /// <summary>
        /// The last antigen the search is actually filtering on, in the
        /// order the criteria are shown. Any is ignored. The results grid
        /// scrolls this column into view so a later criterion is not left
        /// off the row.
        /// </summary>
        public static string? LastActiveAntigen(IEnumerable<AntigenCriterionRow> criteria)
        {
            string? last = null;
            foreach (var criterion in criteria)
            {
                if (criterion.Selected is "+" or "-")
                    last = criterion.Antigen;
            }
            return last;
        }

        /// <summary>
        /// Every antigen the search is filtering on, in the order the
        /// criteria are shown. Any is ignored.
        /// </summary>
        public static IReadOnlyList<string> ActiveAntigens(IEnumerable<AntigenCriterionRow> criteria)
        {
            var antigens = new List<string>();
            foreach (var criterion in criteria)
            {
                if (criterion.Selected is "+" or "-")
                    antigens.Add(criterion.Antigen);
            }
            return antigens;
        }

        private void OpenSelected()
        {
            if (SelectedResult != null)
                OpenResult(SelectedResult);
        }

        private void RunSearch()
        {
            var criteria = Criteria
                .Where(c => c.Selected != "Any")
                .ToDictionary(c => c.Antigen, c => c.Selected);

            if (criteria.Count == 0)
            {
                _main.SetStatus("Please select at least one antigen criterion.");
                return;
            }

            var matches = _db.SearchCellsByProfile(criteria, SelectedZygosity);
            _completedStatus = FormatResultStatus(matches.Count, criteria.Count, SelectedZygosity);
            SelectedResult = null;
            Results.Clear();
            foreach (var (panel, cell) in matches)
                Results.Add(new SearchResultRow(panel, cell));

            PublishResultAntigen(LastActiveAntigen(Criteria), ActiveAntigens(Criteria));
            _main.SetStatus(ScreenStatus.Search(_completedStatus));
        }

        /// <summary>
        /// Status after a search. Homozygous and heterozygous are named so a
        /// smaller count is not read as an ordinary antigen-positive match.
        /// </summary>
        public static string FormatResultStatus(int cells, int criteria, string? zygosity)
        {
            var word = criteria == 1 ? "criterion" : "criteria";
            var line = $"Found {cells} matching cells for {criteria} {word}.";
            if (string.Equals(zygosity, AntigenConstants.ZygosityHomozygous, StringComparison.OrdinalIgnoreCase))
                return line + " Positive cells are homozygous.";
            if (string.Equals(zygosity, AntigenConstants.ZygosityHeterozygous, StringComparison.OrdinalIgnoreCase))
                return line + " Positive cells are heterozygous.";
            return line;
        }

        private void ClearCriteria()
        {
            foreach (var c in Criteria) c.Selected = "Any";
            SelectedZygosity = AntigenConstants.ZygosityBoth;
            _completedStatus = "Search criteria cleared.";
            SelectedResult = null;
            Results.Clear();
            PublishResultAntigen(null, null);
            _main.SetStatus(ScreenStatus.Search(_completedStatus));
        }

        private string? _resultAntigen;

        /// <summary>
        /// Last antigen the latest search filtered on. The results grid
        /// scrolls this column into view. Empty after Clear.
        /// </summary>
        public string? ResultAntigen => _resultAntigen;

        private IReadOnlyList<string> _resultAntigens = Array.Empty<string>();

        /// <summary>
        /// Antigens the latest search filtered on, in criteria order.
        /// When there are several, the results grid shows them first.
        /// Empty after Clear.
        /// </summary>
        public IReadOnlyList<string> ResultAntigens => _resultAntigens;

        /// <summary>
        /// Several search antigens lead the result row so the earlier one
        /// stays beside the later one. One antigen keeps the usual order.
        /// </summary>
        public static IReadOnlyList<string> ResultColumnOrder(
            IReadOnlyList<string> columns,
            IReadOnlyList<string>? searchAntigens) =>
            PanelsViewModel.PlaceSearchAntigensFirst(columns, searchAntigens);

        /// <summary>
        /// How many leading antigen columns stay on screen with the panel,
        /// lot, and cell when the row is scrolled. Two or more search
        /// antigens stay. One antigen, or a cleared search, leaves only
        /// the panel, lot, and cell frozen.
        /// </summary>
        public static int FrozenAntigenColumnCount(
            IReadOnlyList<string> columns,
            IReadOnlyList<string>? searchAntigens)
        {
            if (searchAntigens == null || searchAntigens.Count < 2)
                return 0;
            var order = ResultColumnOrder(columns, searchAntigens);
            var wanted = new HashSet<string>(searchAntigens, StringComparer.Ordinal);
            var count = 0;
            foreach (var column in order)
            {
                if (!wanted.Contains(column)) break;
                count++;
            }
            return count >= 2 ? count : 0;
        }

        /// <summary>
        /// Warehouse antigens with no typed value on any matching cell.
        /// NT is not a typing, so a column that is NT on every match is
        /// still left off. An empty result list hides nothing, so Clear
        /// keeps the usual columns. A typed +, ++, or minus keeps the column.
        /// </summary>
        public static IReadOnlyList<string> BlankWarehouseColumns(
            IReadOnlyList<string> warehouse,
            IEnumerable<SearchResultRow> rows)
        {
            var list = rows as ICollection<SearchResultRow> ?? rows.ToList();
            if (list.Count == 0)
                return Array.Empty<string>();

            var blank = new List<string>();
            foreach (var antigen in warehouse)
            {
                var typed = false;
                foreach (var row in list)
                {
                    if (row.Extra.TryGetValue(antigen, out var value) && IsTypedDisplay(value))
                    {
                        typed = true;
                        break;
                    }
                }
                if (!typed)
                    blank.Add(antigen);
            }
            return blank;
        }

        /// <summary>
        /// A result-grid mark that is a stored typing. Blank and NT are not.
        /// </summary>
        public static bool IsTypedDisplay(string? value) =>
            !string.IsNullOrWhiteSpace(value) &&
            !string.Equals(value, AntigenConstants.AntigenNotTested, StringComparison.Ordinal);

        private int _resultsRevision;

        /// <summary>
        /// Bumps after each search or clear so a repeat of the same antigen
        /// still scrolls the results grid.
        /// </summary>
        public int ResultsRevision => _resultsRevision;

        private void PublishResultAntigen(string? antigen, IReadOnlyList<string>? antigens)
        {
            _resultAntigen = antigen;
            _resultAntigens = antigens ?? Array.Empty<string>();
            OnPropertyChanged(nameof(ResultAntigen));
            OnPropertyChanged(nameof(ResultAntigens));
            _resultsRevision++;
            OnPropertyChanged(nameof(ResultsRevision));
        }

        /// <summary>
        /// Status for this screen. A finished search is repeated so leaving
        /// and returning does not show another screen's line. A selected
        /// row is named after that count.
        /// </summary>
        public void AnnounceStatus() =>
            _main.SetStatus(ScreenStatus.Search(_completedStatus, FormatSelectedResult(SelectedResult)));

        public static string? FormatSelectedResult(SearchResultRow? row)
        {
            if (row == null || string.IsNullOrEmpty(row.PanelName) || string.IsNullOrEmpty(row.CellNumber))
                return null;
            var lot = string.IsNullOrEmpty(row.LotNumber) ? "" : $" ({row.LotNumber})";
            return $"{row.PanelName}{lot} cell {row.CellNumber}";
        }

        private static string? ResultKey(SearchResultRow? row) =>
            row == null ? null : $"{row.PanelName}\u001f{row.LotNumber}\u001f{row.CellNumber}";
    }

    public class AntigenCriterionRow : BaseViewModel
    {
        public string Antigen { get; }
        public static string[] Options => new[] { "Any", "+", "-" };
        public ICommand CycleCommand { get; }

        private string _selected = "Any";
        public string Selected
        {
            get => _selected;
            set
            {
                if (SetField(ref _selected, value))
                    OnPropertyChanged(nameof(ChipLabel));
            }
        }

        public string ChipLabel => Selected == "Any" ? Antigen : $"{Antigen} {Selected}";

        public AntigenCriterionRow(string antigen)
        {
            Antigen = antigen;
            CycleCommand = new RelayCommand(Cycle);
        }

        public void Cycle()
        {
            Selected = Selected switch
            {
                "Any" => "+",
                "+" => "-",
                _ => "Any"
            };
        }
    }

    public class SearchResultRow
    {
        public int PanelId { get; }
        public string PanelName { get; }
        public string? LotNumber { get; }
        public string CellNumber { get; }

        // Antigen values
        public string D { get; }
        public string C { get; }
        public string c { get; }
        public string E { get; }
        public string e { get; }
        public string f { get; }
        public string Cw { get; }
        public string V { get; }
        public string K { get; }
        public string k { get; }
        public string Kpa { get; }
        public string Kpb { get; }
        public string Jsa { get; }
        public string Jsb { get; }
        public string Jka { get; }
        public string Jkb { get; }
        public string Fya { get; }
        public string Fyb { get; }
        public string Lea { get; }
        public string Leb { get; }
        public string M { get; }
        public string N { get; }
        public string S { get; }
        public string s { get; }
        public string Lua { get; }
        public string Lub { get; }
        public string Xga { get; }
        public string P1 { get; }
        public IReadOnlyDictionary<string, string> Extra { get; }

        public SearchResultRow(Panel panel, PanelCell cell)
        {
            PanelId = panel.PanelId;
            PanelName = panel.Name;
            LotNumber = panel.LotNumber;
            CellNumber = cell.CellNumber;
            D = DisplayTyping(cell, "D"); C = DisplayTyping(cell, "C"); c = DisplayTyping(cell, "c");
            E = DisplayTyping(cell, "E"); e = DisplayTyping(cell, "e"); f = DisplayTyping(cell, "f");
            Cw = DisplayTyping(cell, "Cw"); V = DisplayTyping(cell, "V");
            K = DisplayTyping(cell, "K"); k = DisplayTyping(cell, "k");
            Kpa = DisplayTyping(cell, "Kpa"); Kpb = DisplayTyping(cell, "Kpb");
            Jsa = DisplayTyping(cell, "Jsa"); Jsb = DisplayTyping(cell, "Jsb");
            Jka = DisplayTyping(cell, "Jka"); Jkb = DisplayTyping(cell, "Jkb");
            Fya = DisplayTyping(cell, "Fya"); Fyb = DisplayTyping(cell, "Fyb");
            Lea = DisplayTyping(cell, "Lea"); Leb = DisplayTyping(cell, "Leb");
            M = DisplayTyping(cell, "M"); N = DisplayTyping(cell, "N");
            S = DisplayTyping(cell, "S"); s = DisplayTyping(cell, "s");
            Lua = DisplayTyping(cell, "Lua"); Lub = DisplayTyping(cell, "Lub");
            Xga = DisplayTyping(cell, "Xga"); P1 = DisplayTyping(cell, "P1");
            Extra = AntigenConstants.WarehouseAntigens.ToDictionary(
                ag => ag, ag => DisplayTyping(cell, ag));
        }

        private static string DisplayTyping(PanelCell cell, string antigen) =>
            cell.GetTypedValue(antigen) ?? AntigenConstants.AntigenNotTested;
    }
}
