using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using AntibodyPanels.Models;
using AntibodyPanels.ViewModels;

namespace AntibodyPanels.Views
{
    public partial class PanelsView : UserControl
    {
        public static readonly string[] AntigenValues =
            { "+", AntigenConstants.AntigenHomozygous, "-", AntigenConstants.AntigenNotTested };

        private bool _columnsInjected;
        private bool _applyingColumnOrder;
        private PanelsViewModel? _vm;
        private readonly List<DataGridColumn> _antigenColumns = new();

        public PanelsView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_vm != null)
                _vm.PropertyChanged -= OnViewModelPropertyChanged;
            _vm = e.NewValue as PanelsViewModel;
            if (_vm != null)
            {
                _vm.PropertyChanged += OnViewModelPropertyChanged;
                RebuildAntigenColumns();
            }
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PanelsViewModel.SelectedPanel) ||
                e.PropertyName == nameof(PanelsViewModel.AntigenColumnsRevision) ||
                e.PropertyName == nameof(PanelsViewModel.IsEditingAntigens))
                RebuildAntigenColumns();
            if (e.PropertyName == nameof(PanelsViewModel.SelectedPanel) ||
                e.PropertyName == nameof(PanelsViewModel.ListReveal))
                RevealSelectedPanel();
            if (e.PropertyName == nameof(PanelsViewModel.FocusCellNumber) ||
                e.PropertyName == nameof(PanelsViewModel.FocusAntigen) ||
                e.PropertyName == nameof(PanelsViewModel.AntigenColumnsRevision))
                RevealFocusedCell();
        }

        private void AntigenGrid_Loaded(object sender, RoutedEventArgs e)
        {
            if (_columnsInjected) return;
            _columnsInjected = true;
            RebuildAntigenColumns();
        }

        private void RebuildAntigenColumns()
        {
            if (!_columnsInjected) return;

            _applyingColumnOrder = true;
            try
            {
                if (AntigenGrid.FrozenColumnCount != 1)
                    AntigenGrid.FrozenColumnCount = 1;

                foreach (var col in _antigenColumns)
                    AntigenGrid.Columns.Remove(col);
                _antigenColumns.Clear();

                if (_vm == null) return;

                var centeredText = new Style(typeof(TextBlock));
                centeredText.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Center));
                centeredText.Setters.Add(new Setter(TextBlock.FontWeightProperty, FontWeights.SemiBold));
                centeredText.Setters.Add(new Setter(TextBlock.FontSizeProperty, 13.0));

                foreach (var ag in _vm.GridAntigens)
                {
                    var named = AntigenConstants.IsStandard(ag);
                    var col = new DataGridTextColumn
                    {
                        Header = ag,
                        Width = ag.Length >= 3 ? 48 : 40,
                        IsReadOnly = true,
                        Binding = new Binding(named ? ag : $"AntigenValues[{ag}]"),
                        ElementStyle = centeredText,
                        CellStyle = CreateAntigenCellStyle(ag, namedProperty: named),
                    };
                    AntigenGrid.Columns.Add(col);
                    _antigenColumns.Add(col);
                }

                var frozenAntigens = _vm.IsEditingAntigens
                    ? 0
                    : SearchViewModel.FrozenAntigenColumnCount(_vm.GridAntigens, _vm.FocusAntigens);
                AntigenGrid.FrozenColumnCount = 1 + frozenAntigens;
            }
            finally
            {
                _applyingColumnOrder = false;
            }
            RevealFocusedCell();
        }

        private void RevealSelectedPanel()
        {
            var panel = _vm?.SelectedPanel;
            if (panel == null) return;
            var id = panel.PanelId;
            // Loaded is too early: the list rebuild and the frozen name and lot
            // columns leave ScrollIntoView on the previous offset, so the last
            // lot stays below the visible rows. Scroll after that layout.
            Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, () => BringPanelRowIntoView(id));
        }

        private void BringPanelRowIntoView(int id)
        {
            if (_vm?.SelectedPanel is not Models.Panel still || still.PanelId != id)
                return;
            var index = PanelList.Items.IndexOf(still);
            if (index < 0) return;

            var frozen = PanelList.FrozenColumnCount;
            if (frozen != 0)
                PanelList.FrozenColumnCount = 0;
            try
            {
                if (FindVisualChild<ScrollViewer>(PanelList) is ScrollViewer viewer)
                {
                    // Item offset. The viewer clamps this so the last row stays fully on screen.
                    viewer.ScrollToVerticalOffset(index);
                    PanelList.UpdateLayout();
                }
                PanelList.ScrollIntoView(still);
                PanelList.UpdateLayout();
                if (PanelList.ItemContainerGenerator.ContainerFromItem(still) is FrameworkElement row)
                    row.BringIntoView();
            }
            finally
            {
                if (PanelList.FrozenColumnCount != frozen)
                    PanelList.FrozenColumnCount = frozen;
            }
        }

        private void RevealFocusedCell()
        {
            if (_vm?.FocusCellNumber is not string number || AntigenGrid.Columns.Count == 0)
                return;
            var row = _vm.CellRows.FirstOrDefault(r =>
                string.Equals(r.CellNumber, number, StringComparison.Ordinal));
            if (row == null) return;

            var column = ColumnFor(_vm.FocusAntigen)
                ?? AntigenGrid.Columns.FirstOrDefault(c => (c.Header as string) == "Cell")
                ?? AntigenGrid.Columns[0];
            // The tab change and column rebuild have not laid the grid out yet.
            // Scroll after that pass so a far antigen is actually in the viewport.
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () =>
            {
                if (!string.Equals(_vm?.FocusCellNumber, number, StringComparison.Ordinal))
                    return;
                if (ColumnFor(_vm?.FocusAntigen) is DataGridColumn still)
                    column = still;
                AntigenGrid.ScrollIntoView(row, column);
                AntigenGrid.CurrentCell = new DataGridCellInfo(row, column);
                AntigenGrid.SelectedCells.Clear();
                AntigenGrid.SelectedCells.Add(AntigenGrid.CurrentCell);
                AntigenGrid.UpdateLayout();
                CellFor(row, column)?.BringIntoView();
            });
        }

        private DataGridColumn? ColumnFor(string? antigen)
        {
            if (string.IsNullOrEmpty(antigen)) return null;
            return AntigenGrid.Columns.FirstOrDefault(c =>
                string.Equals(c.Header as string, antigen, StringComparison.Ordinal));
        }

        private DataGridCell? CellFor(object item, DataGridColumn column)
        {
            if (AntigenGrid.ItemContainerGenerator.ContainerFromItem(item) is not DataGridRow row)
                return null;
            var presenter = FindVisualChild<DataGridCellsPresenter>(row);
            if (presenter == null)
            {
                row.ApplyTemplate();
                presenter = FindVisualChild<DataGridCellsPresenter>(row);
            }
            return presenter?.ItemContainerGenerator.ContainerFromIndex(column.DisplayIndex) as DataGridCell;
        }

        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            var count = VisualTreeHelper.GetChildrenCount(parent);
            for (var i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T match) return match;
                var nested = FindVisualChild<T>(child);
                if (nested != null) return nested;
            }
            return null;
        }

        private void AntigenGrid_ColumnDisplayIndexChanged(object sender, DataGridColumnEventArgs e)
        {
            if (_applyingColumnOrder || _vm == null || !_vm.IsEditingAntigens) return;

            var ordered = AntigenGrid.Columns
                .Where(c => c.Header is string h && IsAntigenColumn(h))
                .OrderBy(c => c.DisplayIndex)
                .Select(c => (string)c.Header)
                .ToList();
            if (ordered.Count == 0) return;
            _vm.ReplaceAntigenDisplayOrder(ordered);
        }

        private Style CreateAntigenCellStyle(string antigen, bool namedProperty)
        {
            var path = namedProperty ? antigen : $"AntigenValues[{antigen}]";
            var style = new Style(typeof(DataGridCell));
            style.Setters.Add(new Setter(HorizontalContentAlignmentProperty, HorizontalAlignment.Center));
            style.Setters.Add(new Setter(VerticalContentAlignmentProperty, VerticalAlignment.Center));
            style.Setters.Add(new Setter(ForegroundProperty, Brush("AntigenCellTextBrush")));
            style.Setters.Add(new Setter(BorderThicknessProperty, new Thickness(0)));
            style.Setters.Add(new Setter(PaddingProperty, new Thickness(0)));
            style.Setters.Add(new Setter(FocusVisualStyleProperty, null));

            var plus = new DataTrigger { Binding = new Binding(path), Value = "+" };
            plus.Setters.Add(new Setter(BackgroundProperty, Brush("AntigenPositiveBrush")));
            style.Triggers.Add(plus);

            var homo = new DataTrigger { Binding = new Binding(path), Value = AntigenConstants.AntigenHomozygous };
            homo.Setters.Add(new Setter(BackgroundProperty, Brush("AntigenPositiveBrush")));
            style.Triggers.Add(homo);

            var minus = new DataTrigger { Binding = new Binding(path), Value = "-" };
            minus.Setters.Add(new Setter(BackgroundProperty, Brush("AntigenNegativeBrush")));
            style.Triggers.Add(minus);

            var selected = new Trigger { Property = DataGridCell.IsSelectedProperty, Value = true };
            selected.Setters.Add(new Setter(BorderBrushProperty, Brush("AntigenSelectedBorderBrush")));
            selected.Setters.Add(new Setter(BorderThicknessProperty, new Thickness(1)));
            style.Triggers.Add(selected);

            return style;
        }

        private Brush Brush(string key) => (Brush)FindResource(key);

        private void AntigenGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount != 1) { e.Handled = true; return; }

            var cell = FindAncestor<DataGridCell>(e.OriginalSource as DependencyObject);
            if (cell?.Column?.Header is not string antigen || !IsAntigenColumn(antigen))
                return;
            if (cell.DataContext is not PanelCellRow row)
                return;

            AntigenGrid.CurrentCell = new DataGridCellInfo(row, cell.Column);
            AntigenGrid.SelectedCells.Clear();
            AntigenGrid.SelectedCells.Add(AntigenGrid.CurrentCell);

            if (DataContext is PanelsViewModel vm && vm.IsEditingAntigens)
                row.ToggleAntigen(antigen);
            e.Handled = true;
        }

        private void AntigenGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key is not (Key.Enter or Key.Return or Key.Space))
                return;
            if (ToggleCurrentCell())
                e.Handled = true;
        }

        private bool ToggleCurrentCell()
        {
            var current = AntigenGrid.CurrentCell;
            if (current.Item is not PanelCellRow row)
                return false;
            if (current.Column?.Header is not string antigen || !IsAntigenColumn(antigen))
                return false;
            if (DataContext is not PanelsViewModel vm || !vm.IsEditingAntigens)
                return false;

            row.ToggleAntigen(antigen);
            return true;
        }

        private static bool IsAntigenColumn(string header) =>
            header != "Cell" && AntigenConstants.IsKnown(header);

        private static T? FindAncestor<T>(DependencyObject? current) where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T match) return match;
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }
    }
}
