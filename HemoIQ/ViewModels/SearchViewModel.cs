using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using HemoIQ.Data;
using HemoIQ.Models;

namespace HemoIQ.ViewModels
{
    public class SearchViewModel : BaseViewModel
    {
        private readonly DatabaseService _db;
        private readonly MainViewModel? _main;

        public ObservableCollection<AntigenCriterionRow> Criteria { get; } = new();
        public ObservableCollection<SearchResultRow> Results { get; } = new();
        public IReadOnlyList<string> ZygosityOptions { get; } = AntigenConstants.PositiveZygosityOptions;

        public ICommand SearchCommand { get; }
        public ICommand OpenCommand { get; }
        public ICommand ClearCommand { get; }

        // ── Investigation search ───────────────────────────────────────
        public ObservableCollection<Services.InvestigationSearchRow> InvestigationResults { get; } = new();
        public ICommand InvestigationSearchCommand { get; }
        public ICommand OpenInvestigationCommand { get; }

        private string _investigationQuery = string.Empty;
        public string InvestigationQuery
        {
            get => _investigationQuery;
            set => SetField(ref _investigationQuery, value);
        }

        private System.DateTime? _fromDate;
        public System.DateTime? FromDate
        {
            get => _fromDate;
            set => SetField(ref _fromDate, value);
        }

        private System.DateTime? _toDate;
        public System.DateTime? ToDate
        {
            get => _toDate;
            set => SetField(ref _toDate, value);
        }

        private Services.InvestigationSearchRow? _selectedInvestigation;
        public Services.InvestigationSearchRow? SelectedInvestigation
        {
            get => _selectedInvestigation;
            set => SetField(ref _selectedInvestigation, value);
        }

        private string _investigationMessage = "Search by accession, MRN, patient name, DOB, final antibody, or technologist initials.";
        public string InvestigationMessage
        {
            get => _investigationMessage;
            private set => SetField(ref _investigationMessage, value);
        }

        /// <summary>The query behind the investigation results now shown, so opening the tab can run it again.</summary>
        private (string? Query, DateTime? From, DateTime? To)? _lastInvestigationSearch;

        /// <summary>The antigen criteria and zygosity behind the cell results now shown.</summary>
        private (Dictionary<string, string> Criteria, string Zygosity)? _lastCellSearch;

        /// <summary>
        /// Opening the Search tab runs the searches behind the results on screen again, with the criteria they were
        /// run with (not criteria edited since), so a lot deactivated or expired, or an investigation confirmed or
        /// cancelled on another tab, is not picked from an older list. The selected row is kept when it still matches.
        /// </summary>
        public void OnTabSelected()
        {
            if (_lastCellSearch is { } cells)
            {
                var key = ResultKey(SelectedResult);
                ExecuteCellSearch(cells.Criteria, cells.Zygosity, announce: false);
                SelectedResult = Results.FirstOrDefault(r => ResultKey(r) == key);
            }
            if (_lastInvestigationSearch is { } inv)
            {
                var accession = SelectedInvestigation?.Accession;
                ExecuteInvestigationSearch(inv.Query, inv.From, inv.To, announce: false);
                SelectedInvestigation = InvestigationResults.FirstOrDefault(r => r.Accession == accession);
            }
        }

        public void RunInvestigationSearch() =>
            ExecuteInvestigationSearch(InvestigationQuery, FromDate, ToDate, announce: true);

        private void ExecuteInvestigationSearch(string? query, DateTime? fromDate, DateTime? toDate, bool announce)
        {
            InvestigationResults.Clear();
            _lastInvestigationSearch = null;
            if (string.IsNullOrWhiteSpace(query) && fromDate == null && toDate == null)
            {
                InvestigationMessage = "Enter search text or a date range.";
                return;
            }
            if (fromDate != null && toDate != null && fromDate > toDate)
            {
                InvestigationMessage = "The From date is after the To date.";
                return;
            }
            foreach (var row in Services.InvestigationSearch.Search(_db, query, fromDate, toDate))
                InvestigationResults.Add(row);
            _lastInvestigationSearch = (query, fromDate, toDate);
            InvestigationMessage = InvestigationResults.Count == 0
                ? "No investigations match."
                : $"{InvestigationResults.Count} investigation(s), newest first. Double-click or Open to view.";
            if (announce) _main?.SetStatus(InvestigationMessage);
        }

        private void OpenInvestigation()
        {
            if (SelectedInvestigation == null) return;
            _main?.NavigateToWorklistItem(new WorklistItem
            {
                AccessionNumber = SelectedInvestigation.Accession,
                TargetTab = "Specimens",
            });
        }

        private string? _completedStatus;

        private SearchResultRow? _selectedResult;
        public SearchResultRow? SelectedResult
        {
            get => _selectedResult;
            set
            {
                var previous = ResultKey(_selectedResult);
                if (!SetField(ref _selectedResult, value)) return;
                if (_main?.SelectedTabIndex == 7 && previous != ResultKey(_selectedResult))
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

        public SearchViewModel(DatabaseService db, MainViewModel? main = null)
        {
            _db = db;
            _main = main;

            foreach (var ag in AntigenConstants.AllKnownAntigens)
                Criteria.Add(new AntigenCriterionRow(ag));

            SearchCommand = new RelayCommand(RunSearch);
            InvestigationSearchCommand = new RelayCommand(RunInvestigationSearch);
            OpenInvestigationCommand = new RelayCommand(OpenInvestigation, () => SelectedInvestigation != null);
            OpenCommand = new RelayCommand(OpenSelected, () => SelectedResult != null);
            ClearCommand = new RelayCommand(ClearCriteria);
        }

        public void OpenResult(SearchResultRow row) =>
            _main?.OpenPanel(row.PanelId, row.CellNumber, LastActiveAntigen(Criteria), ActiveAntigens(Criteria));

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
                _main?.SetStatus("Please select at least one antigen criterion.");
                return;
            }
            ExecuteCellSearch(criteria, SelectedZygosity, announce: true);
        }

        private void ExecuteCellSearch(Dictionary<string, string> criteria, string zygosity, bool announce)
        {
            var matches = _db.SearchCellsByProfile(criteria, zygosity);
            _lastCellSearch = (criteria, zygosity);
            _completedStatus = FormatResultStatus(matches.Count, criteria.Count, zygosity,
                matches.Count(m => SearchResultRow.LotStatus(m.panel, DateTime.Today) != null));
            SelectedResult = null;
            Results.Clear();
            foreach (var (panel, cell) in matches)
                Results.Add(new SearchResultRow(panel, cell));

            if (!announce) return;
            PublishResultAntigen(LastActiveAntigen(Criteria), ActiveAntigens(Criteria));
            _main?.SetStatus(ScreenStatus.Search(_completedStatus));
        }

        /// <summary>
        /// Status after a search. Homozygous and heterozygous are named so a
        /// smaller count is not read as an ordinary antigen-positive match.
        /// </summary>
        public static string FormatResultStatus(int cells, int criteria, string? zygosity, int notInUse = 0)
        {
            var word = criteria == 1 ? "criterion" : "criteria";
            var line = $"Found {cells} matching cells for {criteria} {word}.";
            if (notInUse > 0)
                line += $" {notInUse} {(notInUse == 1 ? "is" : "are")} on an inactive or expired lot (marked in the Lot column).";
            if (string.Equals(zygosity, AntigenConstants.ZygosityHomozygous, StringComparison.Ordinal))
                return line + " Positive cells are homozygous.";
            if (string.Equals(zygosity, AntigenConstants.ZygosityHeterozygous, StringComparison.Ordinal))
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
            _lastCellSearch = null;
            PublishResultAntigen(null, null);
            _main?.SetStatus(ScreenStatus.Search(_completedStatus));
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
            _main?.SetStatus(ScreenStatus.Search(_completedStatus, FormatSelectedResult(SelectedResult)));

        public static string? FormatSelectedResult(SearchResultRow? row)
        {
            if (row == null || string.IsNullOrEmpty(row.PanelName) || string.IsNullOrEmpty(row.CellNumber))
                return null;
            var lot = string.IsNullOrEmpty(row.LotDisplay) ? "" : $" ({row.LotDisplay})";
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
        /// <summary>The lot as shown: the lot number, then "inactive" or "expired (date)" when it is not in use.</summary>
        public string? LotDisplay { get; }
        public bool LotInUse { get; }
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

        /// <summary>Null for a lot in use; otherwise "expired (date)" or "inactive". Expired wins: it is the reason.</summary>
        public static string? LotStatus(Panel panel, DateTime today) =>
            ExpirationStatus.IsExpired(panel.ExpirationDate, today) ? $"expired {panel.ExpirationDate}"
            : !panel.IsActive ? "inactive"
            : null;

        public SearchResultRow(Panel panel, PanelCell cell)
        {
            PanelId = panel.PanelId;
            PanelName = panel.Name;
            LotNumber = panel.LotNumber;
            var status = LotStatus(panel, DateTime.Today);
            LotInUse = status == null;
            LotDisplay = status == null ? panel.LotNumber
                : string.IsNullOrEmpty(panel.LotNumber) ? status : $"{panel.LotNumber} · {status}";
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
