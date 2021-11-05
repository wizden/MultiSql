using System;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using MultiSql.ViewModels;

namespace MultiSql.Views
{
    /// <summary>
    /// Interaction logic for DatabaseResultsTabItemView.xaml
    /// </summary>
    public partial class DatabaseResultsTabItemView : UserControl
    {
        public DatabaseResultsTabItemView()
        {
            InitializeComponent();
        }

        /// <summary>
        ///     Add a row number to the left-most column on the data grid.
        /// </summary>
        /// <param name="sender">The sender object.</param>
        /// <param name="e">The DataGridRowEventArgs object.</param>
        private void ResultGrid_OnLoadingRow(Object? sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }

        /// <summary>
        ///     Ensure correct display of column name for DataGrid - see
        ///     http://stackoverflow.com/questions/9403782/first-underscore-in-a-datagridcolumnheader-gets-removed
        /// </summary>
        /// <param name="sender">The sender data grid object.</param>
        /// <param name="e">The DataGridAutoGeneratingColumnEventArgs object.</param>
        private void ResultGrid_OnAutoGeneratingColumn(Object? sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            e.Column.SortMemberPath = e.PropertyName;
            var dataGridBoundColumn = e.Column as DataGridBoundColumn;
            dataGridBoundColumn.Binding = new Binding("[" + e.PropertyName + "]");
            e.Column.Header             = e.Column.Header.ToString().Replace("_", "__");
        }

        private void ResultGrid_OnMouseLeftButtonUp(Object sender, MouseButtonEventArgs e)
        {
            ((DatabaseResultsTabItemViewModel) DataContext).SelectedDataTableCount = ((DataGrid) sender).Items.Count;
        }

    }
}
