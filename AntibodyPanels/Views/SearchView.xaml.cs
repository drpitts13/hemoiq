using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using AntibodyPanels.Models;
using AntibodyPanels.ViewModels;

namespace AntibodyPanels.Views
{
    public partial class SearchView : UserControl
    {
        private SearchViewModel? _vm;
        private readonly List<string> _antigenColumnOrder = new();

        public SearchView()
        {
            InitializeComponent();
            AddWarehouseColumns();
            ApplyAntigenMarkStyle();
            foreach (var column in ResultsGrid.Columns.Skip(3))
            {
                if (column.Header is string header && header.Length > 0)
                    _antigenColumnOrder.Add(header);
            }
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_vm != null)
                _vm.PropertyChanged -= OnViewModelPropertyChanged;
            _vm = e.NewValue as SearchViewModel;
            if (_vm == null) return;
            _vm.PropertyChanged += OnViewModelPropertyChanged;
            RevealMatchedAntigen();
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SearchViewModel.ResultsRevision))
                RevealMatchedAntigen();
        }

        private void RevealMatchedAntigen()
        {
            var antigen = _vm?.ResultAntigen;
            var revision = _vm?.ResultsRevision;
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () =>
            {
                if (_vm == null || _vm.ResultsRevision != revision) return;
                ApplyWarehouseVisibility();
                ArrangeResultColumns();
                if (ResultsGrid.Items.Count == 0 || ResultsGrid.Columns.Count == 0) return;
                var column = string.IsNullOrEmpty(antigen)
                    ? ResultsGrid.Columns.FirstOrDefault(c => (c.Header as string) == "D")
                    : ResultsGrid.Columns.FirstOrDefault(c =>
                        string.Equals(c.Header as string, antigen, StringComparison.Ordinal));
                column ??= ResultsGrid.Columns[0];
                ResultsGrid.ScrollIntoView(ResultsGrid.Items[0], column);
                ResultsGrid.UpdateLayout();
            });
        }

        /// <summary>
        /// Several search antigens move to the front of the antigen columns
        /// and stay on screen with the panel, lot, and cell when the row
        /// is scrolled. One antigen, or a cleared search, restores the
        /// usual order and leaves only the panel, lot, and cell frozen.
        /// </summary>
        private void ArrangeResultColumns()
        {
            var antigens = _vm?.ResultAntigens;
            if (ResultsGrid.FrozenColumnCount != 3)
                ResultsGrid.FrozenColumnCount = 3;

            var order = SearchViewModel.ResultColumnOrder(_antigenColumnOrder, antigens);
            for (var i = 0; i < order.Count; i++)
            {
                var column = ResultsGrid.Columns.FirstOrDefault(c =>
                    string.Equals(c.Header as string, order[i], StringComparison.Ordinal));
                if (column == null) continue;
                var target = i + 3;
                if (column.DisplayIndex != target)
                    column.DisplayIndex = target;
            }

            var frozen = 3 + SearchViewModel.FrozenAntigenColumnCount(_antigenColumnOrder, antigens);
            if (ResultsGrid.FrozenColumnCount != frozen)
                ResultsGrid.FrozenColumnCount = frozen;
        }

        /// <summary>
        /// A warehouse column that is blank on every matching cell is left
        /// off the row. Clear, or a search with no cells, shows them again.
        /// </summary>
        private void ApplyWarehouseVisibility()
        {
            var hidden = new HashSet<string>(
                SearchViewModel.BlankWarehouseColumns(
                    AntigenConstants.WarehouseAntigens,
                    _vm?.Results ?? Enumerable.Empty<SearchResultRow>()),
                StringComparer.Ordinal);
            foreach (var column in ResultsGrid.Columns)
            {
                if (column.Header is not string header) continue;
                var hide = hidden.Contains(header);
                if (!hide && column.Visibility == Visibility.Visible) continue;
                column.Visibility = hide ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        private void AddWarehouseColumns()
        {
            foreach (var ag in AntigenConstants.WarehouseAntigens)
            {
                ResultsGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = ag,
                    Binding = new Binding($"Extra[{ag}]"),
                    Width = ag.Length >= 3 ? 44 : 40
                });
            }
        }

        private void ApplyAntigenMarkStyle()
        {
            var markStyle = new Style(typeof(TextBlock));
            markStyle.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Center));
            markStyle.Setters.Add(new Setter(TextBlock.FontWeightProperty, FontWeights.SemiBold));
            markStyle.Setters.Add(new Setter(TextBlock.FontSizeProperty, 12.0));

            foreach (var col in ResultsGrid.Columns.Skip(3).OfType<DataGridTextColumn>())
                col.ElementStyle = markStyle;
        }

        private void ResultsRow_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is not DataGridRow { Item: SearchResultRow row }) return;
            if (DataContext is not SearchViewModel vm) return;
            vm.OpenResult(row);
            e.Handled = true;
        }

        private void ResultsGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            if (DataContext is SearchViewModel vm && vm.OpenCommand.CanExecute(null))
                vm.OpenCommand.Execute(null);
            e.Handled = true;
        }
    }
}
