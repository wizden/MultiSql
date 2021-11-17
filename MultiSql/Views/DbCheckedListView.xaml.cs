using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MultiSql.ViewModels;

namespace MultiSql.Views
{
    /// <summary>
    ///     Interaction logic for DbCheckedListView.xaml
    /// </summary>
    public partial class DbCheckedListView : UserControl
    {

        #region Public Properties

        /// <summary>
        ///     Gets or sets the collection of databases to be assigned to the control.
        /// </summary>
        public ObservableCollection<DatabaseViewModel> AllDatabases => (DataContext as DbCheckedListViewModel).AllDatabases;

        /// <summary>
        ///     Gets the connection string builder for the server connection.
        /// </summary>
        public SqlConnectionStringBuilder ConnectionStringBuilder => ((DbCheckedListViewModel) DataContext).ConnectionStringBuilder;

        /// <summary>
        ///     Gets or sets a value indicating whether the query is running.
        /// </summary>
        public Boolean IsQueryRunning
        {
            get => ((DbCheckedListViewModel) DataContext).IsQueryRunning;

            set
            {
                if (DataContext is DbCheckedListViewModel)
                {
                    ((DbCheckedListViewModel) DataContext).IsQueryRunning = value;
                }
            }
        }

        #endregion Public Properties

        #region Public Fields

        /// <summary>
        ///     The static dependency property for the database list object.
        /// </summary>
        public static readonly DependencyProperty DatabaseCheckedListProperty =
            DependencyProperty.Register("DatabaseCheckedList",
                                        typeof(List<DatabaseViewModel>),
                                        typeof(DbCheckedListView),
                                        new PropertyMetadata(default(DatabaseViewModel), OnDatabaseListChanged));

        #endregion Public Fields

        #region Public Constructors

        public DbCheckedListView()
        {
            InitializeComponent();
        }

        #endregion Public Constructors

        #region Private Methods

        /// <summary>
        ///     Static property to update the selected database list object.
        /// </summary>
        /// <param name="d">The user control dependency object.</param>
        /// <param name="e">The DependencyPropertyChangedEventArgs object to get the new value.</param>
        private static void OnDatabaseListChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var source = d as DbCheckedListView;
            d.SetValue(DatabaseCheckedListProperty, e.NewValue);
        }

        /// <summary>
        ///     Show context menu for each database.
        /// </summary>
        /// <param name="sender">The sender object.</param>
        /// <param name="e">The MouseButtonEventArgs object.</param>
        private void OpenSqlMgmtStudio_Click(Object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem && ((MenuItem) sender).Parent is ContextMenu && ((ContextMenu) ((MenuItem) sender).Parent).PlacementTarget is ContentControl)
            {
                if (((ContentControl) ((ContextMenu) ((MenuItem) sender).Parent).PlacementTarget).Content is DatabaseViewModel)
                {
                    var databaseInfo  = ((ContentControl) ((ContextMenu) ((MenuItem) sender).Parent).PlacementTarget).Content as DatabaseViewModel;
                    var mgmtStudioExe = @"C:\Program Files (x86)\Microsoft SQL Server Management Studio 18\Common7\IDE\Ssms.exe";
                    var args          = String.Format("-S {0} -d {1} -E", databaseInfo.Database.ServerName, databaseInfo.DatabaseName);
                    var ssmsProcess   = new Process();
                    ssmsProcess.StartInfo = new ProcessStartInfo(mgmtStudioExe, args);
                    ssmsProcess.Start();
                }
            }
        }

        /// <summary>
        ///     Filter the database list.
        /// </summary>
        /// <param name="sender">The sender object.</param>
        /// <param name="e">The TextCompositionEventArgs object.</param>
        private void TxtFilterDatabaseList_PreviewTextInput(Object sender, TextCompositionEventArgs e)
        {
            var regex = new Regex("[^A-Za-z0-9_]+"); // Allow only A-Z, 0-9
            e.Handled = regex.IsMatch(e.Text);
        }

        #endregion Private Methods

    }
}