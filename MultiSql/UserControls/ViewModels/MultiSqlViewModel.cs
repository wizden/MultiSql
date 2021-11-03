using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Xml;
using System.Xml.Schema;
using MultiSql.Common;
using NLog;
using static MultiSql.Common.MultiSqlSettings;
using MessageBox = System.Windows.MessageBox;
using MessageBoxOptions = System.Windows.MessageBoxOptions;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
using Timer = System.Timers.Timer;

namespace MultiSql.UserControls.ViewModels
{
    public class MultiSqlViewModel : ViewModelBase
    {

        #region Public Constructors

        /// <summary>
        ///     Initialises a new instance of the <see cref="MultiSqlViewModel" /> class.
        /// </summary>
        public MultiSqlViewModel()
        {
            DatabaseListViewModel  =  new DbCheckedListViewModel();
            executionTimer         =  new Timer(1000);
            executionTimer.Elapsed += ExecutionTimer_Elapsed;

            // TODO: Remove at commit
            ResultDisplayType = ResultDisplayType.Text;
        }

        #endregion Public Constructors

        #region Private Fields

        /// <summary>
        ///     Private store for the Logger object.
        /// </summary>
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        ///     Private store for the base path of the application.
        /// </summary>
        private readonly String appBaseDirectory = Directory.GetParent(Environment.CurrentDirectory).Parent.FullName + "\\";

        /// <summary>
        ///     Private store for the number of dashes to show by default between results.
        /// </summary>
        private readonly Int32 defaultDashesToShow = 50;

        /// <summary>
        ///     Private store for the path to the file containing the editor content.
        /// </summary>
        private readonly String editorContentFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "EditorContent.txt");

        /// <summary>
        ///     The timer to measure query execution time.
        /// </summary>
        private readonly Timer executionTimer;

        /// <summary>
        ///     Private store for a lock object to prevent multiple threads accessing the same area of code.
        /// </summary>
        private readonly Object lockObject = new();

        /// <summary>
        ///     Private store for the delimiter character.
        /// </summary>
        private String _delimiterCharacter = String.Empty;

        /// <summary>
        ///     Private store to indicate whether the database is no longer marked for query selection after execution.
        /// </summary>
        private Boolean _deselectOnQueryCompletion;

        /// <summary>
        ///     Private store for the errors in the query or execution.
        /// </summary>
        private String _errors;

        /// <summary>
        ///     Private store to indicate whether empty results are ignored.
        /// </summary>
        private Boolean _ignoreEmptyResults;

        /// <summary>
        ///     Private store indicating whether the results are displayed to a textbox.
        /// </summary>
        private Boolean _isResultsToText;

        /// <summary>
        ///     Private store for the execution progress text.
        /// </summary>
        private String _progressText = String.Empty;

        /// <summary>
        ///     Private store for the content of the editor queries.
        /// </summary>
        private String _queryAllText;

        /// <summary>
        ///     Private store for the time taken to execute the query.
        /// </summary>
        private String _queryExecutionTimeText;

        /// <summary>
        ///     Private store for the content of the query execution results.
        /// </summary>
        private String _resultsText;

        /// <summary>
        ///     Private store to indicate whether the execution should happen sequentially through the list.
        /// </summary>
        private Boolean _runInSequence;

        /// <summary>
        ///     Private store for all the databases.
        /// </summary>
        private ObservableCollection<DbInfo> allDatabases;

        /// <summary>
        ///     Private store for the cancellation token source object.
        /// </summary>
        private CancellationTokenSource cancellationTokenSource;

        /// <summary>
        ///     Private variable to hold the command object to cancel the running query.
        /// </summary>
        private RelayCommand cancelQueryCommand;

        /// <summary>
        ///     Private store for the connection timeout.
        /// </summary>
        private Int32 connectionTimeout = 30;

        /// <summary>
        ///     Private field to get the currently executing command.
        /// </summary>
        private SqlCommand currentlyExecutingCommand;

        /// <summary>
        ///     Private store for the set of schemas for validating database list.
        /// </summary>
        private XmlSchemaSet databaseListSchemas = new();

        /// <summary>
        ///     Private store for the list of tasks to be run for each database.
        /// </summary>
        private List<Task> databaseQueries;

        /// <summary>
        ///     Private store to hold a value if the save operation has been cancelled.
        /// </summary>
        private Boolean filePerDatabaseSaveCancelled;

        /// <summary>
        ///     Private store for the save location of "Files Per Database".
        /// </summary>
        private String fileSaveLocation = String.Empty;

        /// <summary>
        ///     Boolean to indicate whether the queries returning no data are to be displayed.
        /// </summary>
        private Boolean ignoreEmptyResults;

        /// <summary>
        ///     Boolean to indicate whether the databases retrieval is in progress.
        /// </summary>
        private Boolean isDatabaseRetrievalInProgress;

        /// <summary>
        ///     Boolean to indicate whether the current result is the first result.
        /// </summary>
        private Boolean isFirstResultRetrieved;

        /// <summary>
        ///     Private store to determine if the query is running.
        /// </summary>
        private Boolean isQueryRunning;

        /// <summary>
        ///     Private store to retain the width of the left column.
        /// </summary>
        private Double lastLeftColumnWidth;

        /// <summary>
        ///     Private variable to hold the command object to load a SQL query from a file.
        /// </summary>
        private RelayCommand loadQueryCommand;

        /// <summary>
        ///     Private store for the last saved location of filesPerDatabase.
        /// </summary>
        private String previousFileSaveLocation = String.Empty;

        /// <summary>
        ///     Private store for the list of queries to be executed.
        /// </summary>
        private List<String> queriesToExecute = new();

        /// <summary>
        ///     Date time object to determine query execution time.
        /// </summary>
        private DateTime queryExecutionStartDateTime;

        /// <summary>
        ///     The query to be executed by the request.
        /// </summary>
        private String queryToExecute = String.Empty;

        /// <summary>
        ///     Private variable to hold the command object to run the query.
        /// </summary>
        private RelayCommand runQueryCommand;

        /// <summary>
        ///     Private variable to hold the command object to save a SQL query to a file.
        /// </summary>
        private RelayCommand saveQueryCommand;

        /// <summary>
        ///     Private variable to keep track of site count.
        /// </summary>
        private Int32 siteCounter;

        /// <summary>
        ///     Private variable to keep track of the number of sites where the query is to be executed.
        /// </summary>
        private Int32 sitesToRun;

        #endregion Private Fields

        #region Public Properties

        private ResultDisplayType _resultDisplayType;

        /// <summary>
        ///     Gets a store for all the databases.
        /// </summary>
        public ObservableCollection<DbInfo> AllDatabases => DatabaseListViewModel.AllDatabases;

        /// <summary>
        ///     Gets the relay command object to cancel the query.
        /// </summary>
        public RelayCommand CancelQueryCommand
        {
            get { return cancelQueryCommand ??= new RelayCommand(execute => CancelQuery(execute), canExecute => CanCancelQuery(canExecute)); }
        }

        /// <summary>
        ///     Gets the connection timeout value.
        /// </summary>
        public Int32 ConnectionTimeout
        {
            get => connectionTimeout;

            private set => connectionTimeout = value == 0 ? 30 : value;
        }

        public DbCheckedListViewModel DatabaseListViewModel { get; }

        /// <summary>
        ///     Gets or set the delimiter character.
        /// </summary>
        public String DelimiterCharacter
        {
            get => _delimiterCharacter;
            set
            {
                _delimiterCharacter = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        ///     Gets a value indicating whether the delimiter character is available.
        /// </summary>
        public Boolean DelimiterCharacterAvailable
        {
            get
            {
                var retVal = false;

                retVal = ResultDisplayType == ResultDisplayType.CombinedFile     ||
                         ResultDisplayType == ResultDisplayType.DatabaseFileName ||
                         ResultDisplayType == ResultDisplayType.Text             ||
                         ResultDisplayType == ResultDisplayType.TextFirstHeaderOnly;

                return retVal;
            }
        }

        /// <summary>
        ///     Boolean indicating whether the database is no longer marked for query selection after execution.
        /// </summary>
        public Boolean DeselectOnQueryCompletion
        {
            get => _deselectOnQueryCompletion;
            set
            {
                _deselectOnQueryCompletion = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        ///     Gets or sets the list of errors.
        /// </summary>
        public String Errors
        {
            get => _errors;
            set
            {
                _errors = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        ///     Gets the number of databases selected.
        /// </summary>
        public String GetDatabasesSelectedCountText
        {
            get
            {
                return AllDatabases != null
                           ? String.Format("{0} of {1}", AllDatabases.Count(dh => dh.QueryExecutionRequested).ToString(), AllDatabases.Count().ToString())
                           : String.Empty;
            }
        }

        /// <summary>
        ///     Boolean indicating whether empty results are ignored.
        /// </summary>
        public Boolean IgnoreEmptyResults
        {
            get => _ignoreEmptyResults;
            set
            {
                _ignoreEmptyResults = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether the query is currently executing.
        /// </summary>
        public Boolean IsQueryRunning
        {
            get => isQueryRunning;

            set
            {
                isQueryRunning                       = value;
                DatabaseListViewModel.IsQueryRunning = isQueryRunning;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        ///     Boolean indicating whether the results are displayed to a textbox.
        /// </summary>
        public Boolean IsResultsToText
        {
            get => _isResultsToText;
            set
            {
                _isResultsToText = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        ///     Gets the relay command object to load a SQL query from a file.
        /// </summary>
        public RelayCommand LoadQueryCommand
        {
            get { return loadQueryCommand ??= new RelayCommand(execute => LoadQueryFromFile(execute), canExecute => CanLoadQuery(canExecute)); }
        }

        /// <summary>
        ///     Gets the progress made for the execution.
        /// </summary>
        public String ProgressText
        {
            get => _progressText;
            private set
            {
                _progressText = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        ///     Gets the list of queries to be executed.
        /// </summary>
        public List<String> QueriesToExecute
        {
            get
            {
                queriesToExecute.Clear();

                queriesToExecute = QueryAllText.Split(new[] {Environment.NewLine + Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries).
                                                Where(str => str != Environment.NewLine).
                                                ToList();

                // TODO: Pending task of determining query.
                ////if (String.IsNullOrEmpty(TxtQuery.SelectedText))
                ////{
                ////    if (TxtQuery.Text.Contains("@"))
                ////    {
                ////        // The query contains the "@" character. Consider the whole text as a script instead of individual queries.
                ////        queriesToExecute = new[] {TxtQuery.Text}.ToList();
                ////    }
                ////    else
                ////    {
                ////        queriesToExecute = TxtQuery.Text.Split(new[] {Environment.NewLine + Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries).
                ////                                    Where(str => str != Environment.NewLine).
                ////                                    ToList();
                ////    }
                ////}
                ////else
                ////{
                ////    queriesToExecute = TxtQuery.SelectedText.Split(new[] {Environment.NewLine + Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries).
                ////                                Where(str => str != Environment.NewLine).
                ////                                ToList();
                ////}

                return queriesToExecute;
            }
        }

        public String QueryAllText
        {
            get => _queryAllText;
            set
            {
                _queryAllText = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        ///     Gets the time taken to execute the query.
        /// </summary>
        public String QueryExecutionTimeText
        {
            get => _queryExecutionTimeText;
            private set
            {
                _queryExecutionTimeText = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        ///     Gets or sets the display type for the results.
        /// </summary>
        public ResultDisplayType ResultDisplayType
        {
            get => _resultDisplayType;

            set
            {
                _resultDisplayType = value;
                IsResultsToText    = ResultDisplayType != ResultDisplayType.DifferentTabs;
                RaisePropertyChanged();

            }
        }

        /// <summary>
        ///     Gets the list of available display types for results.
        /// </summary>
        public IEnumerable<ResultDisplayType> ResultDisplayTypes => Enum.GetValues(typeof(ResultDisplayType)).Cast<ResultDisplayType>();

        /// <summary>
        ///     Gest the results content of the query execution.
        /// </summary>
        public String ResultsText
        {
            get => _resultsText;
            set
            {
                _resultsText = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        ///     Gets or sets a boolean indicating whether the execution should happen sequentially through the list.
        /// </summary>
        public Boolean RunInSequence
        {
            get => _runInSequence;
            set
            {
                _runInSequence = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        ///     Gets the relay command object to run the query.
        /// </summary>
        public RelayCommand RunQueryCommand
        {
            get { return runQueryCommand ??= new RelayCommand(execute => InitiateQueriesExecution(execute), canExecute => CanRunQuery(canExecute)); }
        }

        /// <summary>
        ///     Gets the relay command object to save a SQL query to a file.
        /// </summary>
        public RelayCommand SaveQueryCommand
        {
            get { return saveQueryCommand ??= new RelayCommand(execute => SaveQueryToFile(execute), canExecute => CanSaveQuery(canExecute)); }
        }

        /// <summary>
        ///     Gets or sets the current progress of sites completed execution.
        /// </summary>
        public Int32 SiteCounter
        {
            get => siteCounter;
            set
            {
                lock (this)
                {
                    siteCounter = value;
                    RaisePropertyChanged();
                }
            }
        }

        /// <summary>
        ///     Gets or sets the number of sites on which the query is to be executed..
        /// </summary>
        public Int32 SitesToRun
        {
            get => sitesToRun;
            set
            {
                sitesToRun = value;
                RaisePropertyChanged();
            }
        }

        #endregion Public Properties

        #region Private Methods

        /////// <summary>
        ///////     Add a row number to the left-most column on the data grid.
        /////// </summary>
        /////// <param name="sender">The sender object.</param>
        /////// <param name="e">The DataGridRowEventArgs object.</param>
        ////private void AddRowNumberOn_LoadingRow(Object sender, DataGridRowEventArgs e)
        ////{
        ///     TODO: Pending Task
        ////    e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        ////}

        /// <summary>
        ///     Add error information to the errors text box.
        /// </summary>
        /// <param name="message">The message to be added.</param>
        private void AddToErrorText(String message)
        {
            if (String.IsNullOrWhiteSpace(message))
            {
                Errors = String.Empty;

                // TODO: Pending Task
                //GrdRowErrors.Height = GridLength.Auto;
            }
            else
            {
                Errors += message + "\r\n\r\n";

                // TODO: Pending Task
                //GrdRowErrors.Height = new GridLength(2, GridUnitType.Star);
                Logger.Error(message);
            }

            var hasErrors = Errors.Trim().Length > 0;

            // TODO: Pending Task
            //BrdrErrors.Visibility = hasErrors ? Visibility.Visible : Visibility.Collapsed;
            //GrdSplitResultsErrors.Visibility = hasErrors ? Visibility.Visible : Visibility.Collapsed;
            //GrdRowErrors.MinHeight = hasErrors ? 50 : 0;
        }

        /// <summary>
        ///     Determine if a query can be cancelled.
        /// </summary>
        /// <param name="parameter">The parameter object.</param>
        /// <returns>Boolean indicating whether the query can be cancelled.</returns>
        private Boolean CanCancelQuery(Object parameter) => isQueryRunning;

        /// <summary>
        ///     Cancel the running query.
        /// </summary>
        /// <param name="parameter">The parameter object.</param>
        private void CancelQuery(Object parameter)
        {
            cancellationTokenSource.Cancel();

            if (currentlyExecutingCommand != null)
            {
                currentlyExecutingCommand.Cancel();
                currentlyExecutingCommand = null;
            }

            executionTimer.Stop();
            ProgressText = "Query cancelled.";
        }

        /// <summary>
        ///     Determine if a SQL file can be loaded with the queries.
        /// </summary>
        /// <param name="parameter">The parameter object.</param>
        /// <returns>Boolean indicating whether a query exists to be run.</returns>
        private Boolean CanLoadQuery(Object parameter) => !isQueryRunning;

        /// <summary>
        ///     Determine if a query exists to be run.
        /// </summary>
        /// <param name="parameter">The parameter object.</param>
        /// <returns>Boolean indicating whether a query exists to be run.</returns>
        private Boolean CanRunQuery(Object parameter)
        {
            return !String.IsNullOrEmpty(QueryAllText) && !isQueryRunning && (AllDatabases?.Any(dh => dh.QueryExecutionRequested) ?? false);
        }

        /// <summary>
        ///     Determine if a query can be saved.
        /// </summary>
        /// <param name="parameter">The parameter object.</param>
        /// <returns>Boolean indicating whether a query can be saved.</returns>
        private Boolean CanSaveQuery(Object parameter) => !String.IsNullOrEmpty(QueryAllText) && !isQueryRunning;

        /// <summary>
        ///     Show execution time for query.
        /// </summary>
        /// <param name="sender">The sender object.</param>
        /// <param name="e">The System.Timers.ElapsedEventArgs object.</param>
        private void ExecutionTimer_Elapsed(Object sender, ElapsedEventArgs e)
        {
            QueryExecutionTimeText = DateTime.UtcNow.Subtract(queryExecutionStartDateTime).ToString(@"hh\:mm\:ss");
        }

        // TODO: Pending Task
        /////// <summary>
        ///////     Ensure correct display of column name for DataGrid - see
        ///////     http://stackoverflow.com/questions/9403782/first-underscore-in-a-datagridcolumnheader-gets-removed
        /////// </summary>
        /////// <param name="sender">The sender data grid object.</param>
        /////// <param name="e">The DataGridAutoGeneratingColumnEventArgs object.</param>
        ////private void DatabaseDataGridResult_AutoGeneratingColumn(Object sender, DataGridAutoGeneratingColumnEventArgs e)
        ////{
        ////    e.Column.SortMemberPath = e.PropertyName;
        ////    var dataGridBoundColumn = e.Column as DataGridBoundColumn;
        ////    dataGridBoundColumn.Binding = new Binding("[" + e.PropertyName + "]");
        ////    e.Column.Header             = e.Column.Header.ToString().Replace("_", "__");
        ////}

        /////// <summary>
        ///////     Event fired on mouse-up on any of the result data grid.
        /////// </summary>
        /////// <param name="sender">The sender object.</param>
        /////// <param name="e">The MouseButtonEventArgs object.</param>
        ////private void DatabaseDataGridResult_MouseUp(Object sender, MouseButtonEventArgs e)
        ////{
        ////    if (sender is DataGrid && e.ChangedButton == MouseButton.Left)
        ////    {
        ////        SetProgressText(String.Format("Rows: {0}", ((DataGrid) sender).Items.Count));
        ////        e.Handled = true;
        ////    }
        ////}


        /////// <summary>
        ///////     Event handler on collapsing the database list expander.
        /////// </summary>
        /////// <param name="sender">The expander sender object.</param>
        /////// <param name="e">The RoutedEventArgs object.</param>
        ////private void ExpanderDatabaseList_Collapsed(Object sender, RoutedEventArgs e)
        ////{
        ////    if (sender is Expander && TxtBlkDatabaseExpander != null)
        ////    {
        ////        if (AllDatabases.Where(dh => dh.QueryExecutionRequested).Count() > 0)
        ////        {
        ////            TxtBlkDatabaseExpander.Text = "Databases (" + GetDatabasesSelectedCountText + ")";
        ////        }
        ////    }

        ////    lastLeftColumnWidth        = ColDefDatabaseList.Width.Value;
        ////    ColDefDatabaseList.Width   = new GridLength(1, GridUnitType.Auto);
        ////    ColDefQueryExecution.Width = new GridLength(1, GridUnitType.Star);
        ////}

        /////// <summary>
        ///////     Event handler on expanding the database list expander.
        /////// </summary>
        /////// <param name="sender">The expander sender object.</param>
        /////// <param name="e">The RoutedEventArgs object.</param>
        ////private void ExpanderDatabaseList_Expanded(Object sender, RoutedEventArgs e)
        ////{
        ////    if (sender is Expander && TxtBlkDatabaseExpander != null)
        ////    {
        ////        TxtBlkDatabaseExpander.Text = "Databases";
        ////    }

        ////    if (lastLeftColumnWidth > 1)
        ////    {
        ////        ColDefDatabaseList.Width = new GridLength(lastLeftColumnWidth, GridUnitType.Pixel);
        ////    }

        ////    ColDefQueryExecution.Width = new GridLength(1, GridUnitType.Star);
        ////}

        /// <summary>
        ///     Gets the estimated number of rows per query based on statistics.
        /// </summary>
        /// <param name="con">The connection object.</param>
        /// <param name="query">The queries to execute.</param>
        /// <returns>Returns the estimated number of rows per query based on statistics.</returns>
        private async Task<List<KeyValuePair<String, Int32>>> GetEstimatedRowsPerQuery(SqlConnection con, String query)
        {
            var queryEstimatedResults = new List<KeyValuePair<String, Int32>>();

            var xmlPlanResults = await GetXmlPlanForQuery(con, query);

            foreach (var xmlPlanResult in xmlPlanResults)
            {
                ////XmlSerializer serializer = new XmlSerializer(typeof(ShowPlanXML));
                ////ShowPlanXML planResult = (ShowPlanXML)serializer.Deserialize(new StringReader(xmlPlanResult));

                ////var queryPlanResults = planResult.BatchSequence.SelectMany(x => x)
                ////    .SelectMany(x => x.Items)
                ////    .OfType<StmtSimpleType>()
                ////    .Select(sst => new { sst.StatementText, sst.StatementEstRows });

                ////foreach (var queryPlanResult in queryPlanResults)
                ////{
                ////    queryEstimatedResults.Add(new KeyValuePair<string, int>(queryPlanResult.StatementText, Convert.ToInt32(queryPlanResult.StatementEstRows)));
                ////}
            }

            return queryEstimatedResults;
        }

        /// <summary>
        ///     Gets the result in text format for the data table.
        /// </summary>
        /// <param name="dataSet">The data set object whose tables are used to get the result.</param>
        /// <param name="resultDisplayType">The display type for the result.</param>
        /// <returns>The result in text format for the data table.</returns>
        private List<Result> GetResultText(DataSet dataSet, ResultDisplayType resultDisplayType)
        {
            var ssmsStyleFormatting = resultDisplayType == ResultDisplayType.TextSqlFormatted;
            var delimiterCharacter  = "\t";
            var queryCounter        = 0;
            var totalRows           = 0;
            var retVal              = new List<Result>();

            if (dataSet != null)
            {
                if (!ssmsStyleFormatting)
                {
                    delimiterCharacter = !String.IsNullOrEmpty(DelimiterCharacter) ? DelimiterCharacter : "\t";
                }
                else
                {
                    delimiterCharacter = " ";
                }

                foreach (DataTable dataTable in dataSet.Tables)
                {
                    totalRows += dataTable.Rows.Count;
                }

                if (totalRows > 0 || !IgnoreEmptyResults)
                {
                    foreach (DataTable dataTable in dataSet.Tables)
                    {
                        var result = new Result();

                        if (dataTable.AsEnumerable().Count() > 0)
                        {
                            var columnMaxLengthDisplay = new Dictionary<String, Int32>();
                            var headerText             = String.Empty;

                            foreach (var column in dataTable.Columns.Cast<DataColumn>())
                            {
                                var maxLength =
                                    dataTable.AsEnumerable().OrderByDescending(r => r.IsNull(column) ? "NULL".Length : r.Field<String>(column).Length).FirstOrDefault()[column].
                                              ToString().
                                              Length;
                                columnMaxLengthDisplay.Add(column.ColumnName, Math.Max(column.ColumnName.Length, maxLength));
                                headerText += column.ColumnName.PadRight(maxLength, ' ') + delimiterCharacter;
                            }

                            result.Header = headerText.Trim();

                            if (ssmsStyleFormatting && columnMaxLengthDisplay.Count > 0)
                            {
                                result.AddRow(String.Join(" ", columnMaxLengthDisplay.Select(cmld => PrintDashes(cmld.Value, "-"))));
                            }

                            var line = new List<String>();

                            foreach (DataRow dr in dataTable.Rows)
                            {
                                foreach (var column in dataTable.Columns.Cast<DataColumn>())
                                {
                                    var valueToAdd = dr.IsNull(column) ? "NULL" : dr[column].ToString();
                                    line.Add(ssmsStyleFormatting
                                                 ? valueToAdd.PadRight(columnMaxLengthDisplay.First(cmld => cmld.Key == column.ColumnName).Value + 1, ' ')
                                                 : valueToAdd + delimiterCharacter);
                                }

                                result.AddRow(String.Join(String.Empty, line).Trim());
                                line.Clear();
                            }

                            queryCounter++;
                        }

                        retVal.Add(result);
                    }
                }
            }
            else
            {
                var emptyResult = new Result();
                emptyResult.AddRow("Failed to get data.");
                retVal.Add(emptyResult);
            }

            return retVal;
        }

        /// <summary>
        ///     Get the table with the columns set.
        /// </summary>
        /// <param name="sdr">The SQL Data Reader object that will provide the schema.</param>
        /// <param name="tableCounter">Counter for table if multiple queries are used.</param>
        /// <returns>The table with the columns set.</returns>
        private DataTable GetTableWithSchema(SqlDataReader sdr, Int32 tableCounter)
        {
            var schemaTable          = sdr.GetSchemaTable();
            var table                = new DataTable("Table" + tableCounter);
            var duplicateColumnNames = new List<String>();

            for (var schemaColumnCounter = 0; schemaColumnCounter < schemaTable.Rows.Count; schemaColumnCounter++)
            {
                try
                {
                    table.Columns.Add(schemaTable.Rows[schemaColumnCounter]["ColumnName"].ToString());
                }
                catch (DuplicateNameException)
                {
                    var duplicateColumn = schemaTable.Rows[schemaColumnCounter]["ColumnName"].ToString();
                    duplicateColumnNames.Add(duplicateColumn);
                    var nextDuplicateColumnName = String.Format("{0}_{1}", duplicateColumn, duplicateColumnNames.Count(c => c.Contains(duplicateColumn)));
                    table.Columns.Add(nextDuplicateColumnName);
                    table.Columns[nextDuplicateColumnName].Caption = duplicateColumn;
                }
            }

            return table;
        }

        /// <summary>
        ///     Gets the XML plan for the queries to execute.
        /// </summary>
        /// <param name="con">The connection object.</param>
        /// <param name="queryText">The queries to execute.</param>
        /// <returns>Returns the XML plan for the queries to execute.</returns>
        private async Task<List<String>> GetXmlPlanForQuery(SqlConnection con, String queryText)
        {
            var result = new List<String>();

            using (var command = new SqlCommand())
            {
                command.Connection     = con;
                command.CommandTimeout = Math.Max(10, con.ConnectionTimeout / 10);
                command.CommandText    = "SET STATISTICS XML ON";

                try
                {
                    await command.ExecuteNonQueryAsync(cancellationTokenSource.Token);

                    command.CommandText = queryText;

                    using (var reader = await command.ExecuteReaderAsync(cancellationTokenSource.Token))
                    {
                        Object lastValue = null;

                        do
                        {
                            if (await reader.ReadAsync(cancellationTokenSource.Token))
                            {
                                lastValue = reader.GetValue(0);

                                if (lastValue.ToString().Contains("ShowPlanXML"))
                                {
                                    result.Add(lastValue.ToString());
                                }
                            }
                        } while (await reader.NextResultAsync(cancellationTokenSource.Token));
                    }
                }
                catch (SqlException sqlEx)
                {
                    if (sqlEx.Message.Contains("Execution Timeout Expired."))
                    {
                        // Don't bother throwing error message as this is just failure to get plan.
                    }
                }
            }

            using (var command = new SqlCommand())
            {
                command.Connection  = con;
                command.CommandText = "SET STATISTICS XML OFF";
                command.ExecuteNonQuery();
            }

            return result;
        }

        /// <summary>
        ///     Gets the XmlReaderSettings for a specified schema.
        /// </summary>
        /// <param name="schemaPath">The path to the schema file.</param>
        /// <param name="validationType">The validation type.</param>
        /// <param name="xmlSchemaValidationFlags">The validation flags.</param>
        /// <param name="ignoreComments">Determine whether the comments in the xml are ignored.</param>
        /// <param name="ignoreWhiteSpace">Determine whether the whitespaces in the xml are ignored.</param>
        /// <returns>The XmlReaderSettings for a specified schema.</returns>
        private XmlReaderSettings GetXmlReaderSettings(String                   schemaPath,
                                                       ValidationType           validationType,
                                                       XmlSchemaValidationFlags xmlSchemaValidationFlags,
                                                       Boolean                  ignoreComments   = true,
                                                       Boolean                  ignoreWhiteSpace = true)
        {
            var retVal = new XmlReaderSettings();
            retVal.Schemas.Add(null, schemaPath);
            retVal.ValidationType   = validationType;
            retVal.ValidationFlags  = xmlSchemaValidationFlags;
            retVal.IgnoreComments   = ignoreComments;
            retVal.IgnoreWhitespace = ignoreWhiteSpace;
            return retVal;
        }

        /// <summary>
        ///     Run the query on the selected sites.
        /// </summary>
        /// <param name="parameter">The parameter object.</param>
        private async void InitiateQueriesExecution(Object parameter)
        {
            File.WriteAllText(editorContentFilePath, QueryAllText);
            queryExecutionStartDateTime = DateTime.UtcNow;
            executionTimer.Start();
            IsQueryRunning = true;
            List<DbInfo> databasesToRun = new();
            AddToErrorText(String.Empty);
            queryToExecute               = String.Join(Environment.NewLine, String.Join(Environment.NewLine + Environment.NewLine, QueriesToExecute));
            databasesToRun               = AllDatabases.Where(dh => dh.QueryExecutionRequested).ToList();
            SitesToRun                   = databasesToRun.Count;
            isFirstResultRetrieved       = false;
            filePerDatabaseSaveCancelled = false;
            SiteCounter                  = 0;

            try
            {
                await RunQueryOnDatabasesAsync(databasesToRun);
            }
            catch (Exception ex)
            {
                AddToErrorText(ex.Message);
            }

            executionTimer.Stop();
            var queryEndTime = DateTime.UtcNow;
            QueryExecutionTimeText = queryEndTime.Subtract(queryExecutionStartDateTime).ToString(@"hh\:mm\:ss");
            IsQueryRunning         = false;
        }

        /// <summary>
        ///     Load a SQL query from a file.
        /// </summary>
        /// <param name="parameter">The parameter object.</param>
        private async void LoadQueryFromFile(Object parameter)
        {
            var ofd = new OpenFileDialog();
            ofd.Filter          = "SQL files (*.sql)|*.sql|All files (*.*)|*.*";
            ofd.CheckPathExists = ofd.CheckFileExists = true;
            ofd.Multiselect     = false;
            var overwriteContents = false;

            if (ofd.ShowDialog().Value)
            {
                try
                {
                    var getFileContents = new Task<String>(() => { return File.ReadAllText(ofd.FileName); });

                    getFileContents.Start();

                    if (!String.IsNullOrEmpty(QueryAllText))
                    {
                        overwriteContents = MessageBox.Show("Overwrite the existing text.",
                                                            "Overwrite text",
                                                            MessageBoxButton.YesNo,
                                                            MessageBoxImage.Question,
                                                            MessageBoxResult.No,
                                                            MessageBoxOptions.None) ==
                                            MessageBoxResult.Yes;

                        if (overwriteContents)
                        {
                            await getFileContents;
                            QueryAllText = getFileContents.Result;
                        }
                    }
                    else
                    {
                        await getFileContents;
                        QueryAllText = getFileContents.Result;
                    }
                }
                catch (IOException iex)
                {
                    MessageBox.Show(iex.Message, ApplicationName);
                }
            }
        }

        //TODO: Pending task
        /////// <summary>
        ///////     Event fired on mouse-up on any of the tab.
        /////// </summary>
        /////// <param name="sender">The sender object.</param>
        /////// <param name="e">The MouseButtonEventArgs object.</param>
        ////private void NewTabPageForDatabase_MouseUp(Object sender, MouseButtonEventArgs e)
        ////{
        ////    if (sender is TabItem && e.ChangedButton == MouseButton.Left)
        ////    {
        ////        var tabItem = (TabItem) sender;
        ////        SetProgressText(String.Format("Rows: {0}", tabItem.Tag));
        ////    }
        ////}

        /// <summary>
        ///     Returns a string containing dashes.
        /// </summary>
        /// <param name="dashCount">The number of dashes to be returned.</param>
        /// <param name="charToPrint">The character to print. Default character is "-".</param>
        /// <returns>A string containing dashes.</returns>
        private String PrintDashes(Int32 dashCount, String charToPrint = "─")
        {
            var retVal = String.Empty;

            retVal = dashCount == 0
                         ? "───────────────────────────────────────"
                         : String.Join(String.Empty, Enumerable.Range(1, dashCount).Select(n => charToPrint));

            return retVal;
        }

        /// <summary>
        ///     Handling row changed event to check for cancelling of command.
        /// </summary>
        /// <param name="sender">The sender object.</param>
        /// <param name="e">The DataRowChangeEventArgs object.</param>
        private void ResultRows_RowChanged(Object sender, DataRowChangeEventArgs e)
        {
            if (cancellationTokenSource != null && cancellationTokenSource.Token != null && cancellationTokenSource.Token.IsCancellationRequested && isQueryRunning)
            {
                IsQueryRunning = false;
                throw new OperationCanceledException("Execution terminated by user.", cancellationTokenSource.Token);
            }
        }

        /// <summary>
        ///     Run each query statement.
        /// </summary>
        /// <param name="con">The connection object.</param>
        /// <param name="query">The query string.</param>
        /// <returns>Dataset containing the tables with the results of the query or queries.</returns>
        private async Task<DataSet> RunIndividualQuery(SqlConnection con, String query)
        {
            var retVal    = new DataSet();
            var dbDetails = $"{con.DataSource} / {con.Database}";
            Logger.Debug($"Running query on: {dbDetails}");
            ////var getQueryEstimatedResultsAsync = this.GetEstimatedRowsPerQuery(con, query);

            if (!cancellationTokenSource.IsCancellationRequested && !String.IsNullOrWhiteSpace(query))
            {
                using (var cmd = new SqlCommand(query, con))
                {
                    cmd.CommandTimeout = con.ConnectionTimeout;

                    using (var sdr = await cmd.ExecuteReaderAsync(cancellationTokenSource.Token))
                    {
                        var tableCounter = 0;
                        ////List<KeyValuePair<string, int>> queryEstimatedResults = await getQueryEstimatedResultsAsync;

                        do
                        {
                            ////if (queryEstimatedResults.Count > 0)
                            ////{
                            ////int estimatedRowCount = queryEstimatedResults[tableCounter].Value;
                            var table = GetTableWithSchema(sdr, tableCounter++);
                            retVal.Tables.Add(table);

                            if (sdr.HasRows)
                            {
                                while (await sdr.ReadAsync())
                                {
                                    var newRow = table.NewRow();

                                    for (var colCounter = 0; colCounter < sdr.FieldCount; colCounter++)
                                    {
                                        newRow[colCounter] = sdr[colCounter];
                                    }

                                    table.Rows.Add(newRow);
                                }

                                Logger.Debug($"Found {table.Rows.Count} rows on {dbDetails}");
                            }
                            ////}
                        } while (await sdr.NextResultAsync());

                        sdr.Close();
                    }
                }

                con.Close();
            }

            Logger.Debug($"Finished query on: {dbDetails}");
            return retVal;
        }

        /// <summary>
        ///     Run the query on all the selected database.
        /// </summary>
        /// <param name="dbInfo">The selected database.</param>
        /// <returns>Boolean indicating whether the query ran to completion.</returns>
        private async Task<Boolean> RunQueryOnDatabase(DbInfo dbInfo)
        {
            DataSet individualQueryResult = null;
            DatabaseListViewModel.ConnectionStringBuilder.InitialCatalog = dbInfo.Database.Replace("__", "_");

            using (var con = new SqlConnection(DatabaseListViewModel.ConnectionStringBuilder.ConnectionString))
            {
                try
                {
                    await con.OpenAsync(cancellationTokenSource.Token);
                    individualQueryResult = await RunIndividualQuery(con, queryToExecute);
                    await SendToDisplay(dbInfo, individualQueryResult);
                }
                catch (AggregateException aex)
                {
                    var errorText = String.Empty;
                    aex.InnerExceptions.ToList().ForEach(ex => errorText += ex.Message);
                    AddToErrorText(String.Format("{0}.{1}\r\n{2}\r\n{3}",
                                                 dbInfo.Server,
                                                 dbInfo.Database.Replace("__", "_"),
                                                 errorText,
                                                 PrintDashes(defaultDashesToShow)));
                }
                catch (SqlException sqlEx)
                {
                    if (!sqlEx.Message.Contains("Operation cancelled by user."))
                    {
                        AddToErrorText(String.Format("{0}.{1}\r\n{2}\r\n{3}",
                                                     dbInfo.Server,
                                                     dbInfo.Database.Replace("__", "_"),
                                                     sqlEx.Message,
                                                     PrintDashes(defaultDashesToShow)));
                    }
                }

                con.Close();
                con.Dispose();

                if (!RunInSequence)
                {
                    var percentComplete =
                        (databaseQueries.Where(t => t.Status == TaskStatus.Canceled ||
                                                    t.Status == TaskStatus.Faulted  ||
                                                    t.Status == TaskStatus.RanToCompletion).
                                         Count() +
                         1) *
                        100 /
                        databaseQueries.Count();
                }
            }

            return true;
        }

        /// <summary>
        ///     Prepare to run the query on all the selected database.
        /// </summary>
        /// <param name="dbInfos">The selected database.</param>
        /// <returns>Task object indicating the completion status.</returns>
        private async Task RunQueryOnDatabasesAsync(List<DbInfo> dbInfos)
        {
            databaseQueries         = new List<Task>();
            cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Token.ThrowIfCancellationRequested();

            ResultsText            = String.Empty;
            ProgressText           = String.Format("Completed {0} of {1}", SiteCounter, SitesToRun);
            fileSaveLocation       = String.Empty;
            QueryExecutionTimeText = String.Empty;

            //TODO: Pending task.
            ////TabMainResults.Items.Clear();
            ////TabMainResults.Visibility = Visibility.Hidden;
            Logger.Debug("Preparing to run query on databases.");

            foreach (var dh in dbInfos.OrderBy(dh => dh.Database).ToList())
            {
                dh.QueryRetryAttempt = 0;

                if (RunInSequence)
                {
                    await RunQueryOnDatabase(dh);
                }
                else
                {
                    databaseQueries.Add(RunQueryOnDatabase(dh));

                    //await Task.Delay(2000); // Fake delay
                }
            }

            try
            {
                if (!RunInSequence)
                {
                    await Task.WhenAll(databaseQueries);
                }

                Logger.Debug("Completed running query on databases.");

                //TODO: Pending task.
                ////if (((ComboBoxItem) CmbResultDisplayMethod.SelectedItem).Content.ToString() == "Different tabs" && TabMainResults.Items != null && TabMainResults.Items.Count > 0)
                ////{
                ////    TabMainResults.SelectedIndex = 0;
                ////}

                ProgressText = String.Empty;
            }
            catch (TaskCanceledException)
            {
                AddToErrorText(String.Format("{0}\r\n{1}", "Query execution cancelled by user.", PrintDashes(0)));
            }
            catch (Exception ex)
            {
                AddToErrorText(String.Format("{0}\r\n{1}", ex.Message, PrintDashes(0)));
            }

            CommandManager.InvalidateRequerySuggested(); // Requery since sometimes the command button does not refresh it's state on completion.
        }

        /// <summary>
        ///     Save a SQL query to a file.
        /// </summary>
        /// <param name="parameter">The parameter object.</param>
        /// <returns>Boolean indicating whether the file was saved.</returns>
        private Boolean SaveQueryToFile(Object parameter)
        {
            var retVal = false;
            var sfd    = new SaveFileDialog();
            sfd.Filter          = "SQL files (*.sql)|*.sql|All files (*.*)|*.*";
            sfd.CheckPathExists = true;

            if (!String.IsNullOrWhiteSpace(QueryAllText) && sfd.ShowDialog().Value)
            {
                try
                {
                    File.WriteAllText(sfd.FileName, QueryAllText);
                    retVal = true;
                }
                catch (IOException iex)
                {
                    MessageBox.Show(iex.Message, ApplicationName);
                    retVal = false;
                }
            }

            ProgressText = String.Empty;
            return retVal;
        }

        /// <summary>
        ///     Send the result to a text file.
        /// </summary>
        /// <param name="results">The result of the query.</param>
        /// <param name="dbInfo">The database object.</param>
        private async Task SendResultToCombinedFile(List<Result> results, DbInfo dbInfo = null)
        {
            if (ResultDisplayType == ResultDisplayType.CombinedFile)
            {
                await Task.Run(() =>
                               {
                                   lock (lockObject)
                                   {
                                       if (String.IsNullOrWhiteSpace(fileSaveLocation) && filePerDatabaseSaveCancelled == false)
                                       {
                                           var sfd = new SaveFileDialog();
                                           sfd.Filter          = "Text Files(*.txt)|*.txt|All(*.*)|*";
                                           sfd.OverwritePrompt = true;

                                           if (!String.IsNullOrWhiteSpace(previousFileSaveLocation))
                                           {
                                               sfd.InitialDirectory = Directory.GetParent(previousFileSaveLocation).FullName;
                                           }

                                           // TODO, get parent window as owner of showdialog.
                                           if (sfd.ShowDialog() == true)
                                           {
                                               fileSaveLocation         = sfd.FileName;
                                               previousFileSaveLocation = fileSaveLocation;
                                               File.Delete(sfd.FileName);
                                           }
                                           else
                                           {
                                               filePerDatabaseSaveCancelled =
                                                   true; // Prevent opening the dialog multiple times if the first save has been cancelled.
                                           }
                                       }
                                   }
                               });


                if (!String.IsNullOrWhiteSpace(fileSaveLocation))
                {
                    var fileContent = new StringBuilder();

                    if (dbInfo != null)
                    {
                        fileContent.AppendLine(dbInfo.Database + Environment.NewLine + PrintDashes(defaultDashesToShow));
                    }

                    foreach (var result in results)
                    {
                        fileContent.AppendLine(result.Header);
                        fileContent.AppendLine(result.Rows);
                        fileContent.AppendLine();
                    }

                    fileContent.AppendLine();

                    lock (lockObject)
                    {
                        File.AppendAllText(Path.Combine(fileSaveLocation, fileSaveLocation), fileContent.ToString());
                    }
                }
            }
        }

        /// <summary>
        ///     Send the result to a text file for each database.
        /// </summary>
        /// <param name="results">The result of the query.</param>
        /// <param name="dbInfo">The database object.</param>
        private async Task SendResultToIndividualFile(List<Result> results, DbInfo dbInfo = null)
        {
            if (ResultDisplayType == ResultDisplayType.DatabaseFileName)
            {
                await Task.Run(() =>
                               {
                                   lock (lockObject)
                                   {
                                       if (String.IsNullOrWhiteSpace(fileSaveLocation) && filePerDatabaseSaveCancelled == false)
                                       {

                                           var fbd = new FolderBrowserDialog();
                                           fbd.Description = "Select folder to store results";
                                           fbd.RootFolder  = Environment.SpecialFolder.MyComputer;

                                           if (!String.IsNullOrWhiteSpace(previousFileSaveLocation))
                                           {
                                               fbd.SelectedPath = Directory.Exists(previousFileSaveLocation)
                                                                      ? previousFileSaveLocation
                                                                      : Directory.GetParent(previousFileSaveLocation).FullName;
                                           }

                                           if (fbd.ShowDialog() == DialogResult.OK)
                                           {
                                               fileSaveLocation         = fbd.SelectedPath;
                                               previousFileSaveLocation = fileSaveLocation;
                                           }
                                           else
                                           {
                                               filePerDatabaseSaveCancelled =
                                                   true; // Prevent opening the dialog multiple times if the first save has been cancelled.
                                           }
                                       }
                                   }
                               });

                if (!String.IsNullOrWhiteSpace(fileSaveLocation))
                {
                    var fileContent = new StringBuilder();

                    foreach (var result in results)
                    {
                        fileContent.AppendLine(result.Header);
                        fileContent.AppendLine(result.Rows);
                        fileContent.AppendLine();
                    }

                    lock (lockObject)
                    {
                        File.WriteAllText(Path.Combine(fileSaveLocation, dbInfo.Database.Replace("__", "_") + ".txt"), fileContent.ToString());
                    }
                }
            }
        }

        //TODO: Pending task.
        /////// <summary>
        ///////     Display the result on separate tabs.
        /////// </summary>
        /////// <param name="dbInfo">The database object.</param>
        /////// <param name="dataSet">The resultant data set whose tables are to be displayed.</param>
        ////private async Task SendResultToTabs(DbInfo dbInfo, DataSet dataSet)
        ////{
        ////    TabMainResults.Visibility = Visibility.Visible;
        ////    var scrVwrGrid                       = new ScrollViewer();
        ////    var databaseDataGridResultsGridPanel = new Grid();
        ////    var totalRows                        = 0;

        ////    if (dataSet != null && dataSet.Tables != null && dataSet.Tables.Count > 0)
        ////    {
        ////        foreach (DataTable dataTable in dataSet.Tables)
        ////        {
        ////            totalRows += dataTable.Rows.Count;
        ////        }

        ////        if (!(IgnoreEmptyResults && totalRows == 0))
        ////        {
        ////            foreach (DataTable dataTable in dataSet.Tables)
        ////            {
        ////                if (databaseDataGridResultsGridPanel.RowDefinitions.Count > 0)
        ////                {
        ////                    // Add grid splitter between every table.
        ////                    databaseDataGridResultsGridPanel.RowDefinitions.Add(new RowDefinition {MaxHeight = 10});
        ////                    var splitter = new GridSplitter {HorizontalAlignment                             = HorizontalAlignment.Stretch, Height = 10};
        ////                    Grid.SetRow(splitter, databaseDataGridResultsGridPanel.RowDefinitions.Count - 1);
        ////                    databaseDataGridResultsGridPanel.Children.Add(splitter);
        ////                }

        ////                // Prevent division by 0 error when calculating percentage of rows for height later.
        ////                Double percentHeight = dataTable.Rows.Count * 100 / (totalRows == 0 ? 1 : totalRows);

        ////                // Add row definition and datagrid for the new row definition.
        ////                databaseDataGridResultsGridPanel.RowDefinitions.Add(new RowDefinition
        ////                                                                    {
        ////                                                                        Height = new GridLength(percentHeight, GridUnitType.Star), MinHeight = 10, MaxHeight = 500
        ////                                                                    });

        ////                var databaseDataGridResult = new DataGrid
        ////                                             {
        ////                                                 CanUserAddRows       = false,
        ////                                                 CanUserResizeColumns = true,
        ////                                                 IsReadOnly           = true,
        ////                                                 MaxHeight            = databaseDataGridResultsGridPanel.RowDefinitions.Last().MaxHeight,
        ////                                                 RowHeaderWidth       = 20
        ////                                             };

        ////                databaseDataGridResult.AutoGeneratingColumn += DatabaseDataGridResult_AutoGeneratingColumn;

        ////                databaseDataGridResult.LoadingRow     += AddRowNumberOn_LoadingRow;
        ////                databaseDataGridResult.RowHeaderWidth =  20 + dataTable.Rows.Count.ToString().Length * 5;
        ////                databaseDataGridResult.ItemsSource    =  dataTable.DefaultView;
        ////                databaseDataGridResult.MouseUp        += DatabaseDataGridResult_MouseUp;
        ////                Grid.SetRow(databaseDataGridResult, databaseDataGridResultsGridPanel.RowDefinitions.Count - 1);
        ////                databaseDataGridResultsGridPanel.Children.Add(databaseDataGridResult);
        ////            }

        ////            // Set the grid as content for the scroll viewer. Set the scroll viewer as content for the tab item.
        ////            scrVwrGrid.Content = databaseDataGridResultsGridPanel;
        ////            var newTabPageForDbInfo = new TabItem {Header = dbInfo.Database, Content = scrVwrGrid, Tag = totalRows.ToString()};
        ////            newTabPageForDbInfo.MouseUp += NewTabPageForDatabase_MouseUp;

        ////            await Task.Run(() => { Dispatcher.Invoke(() => { TabMainResults.Items.Add(newTabPageForDbInfo); }); });
        ////        }
        ////    }
        ////}

        /// <summary>
        ///     Display the result on separate tabs.
        /// </summary>
        /// <param name="results">The result of the query.</param>
        /// <param name="dbInfo">The database object.</param>
        /// <param name="hideHeaders">Hide connection header information.</param>
        private async Task SendResultToText(List<Result> results, DbInfo dbInfo, Boolean hideHeaders = false)
        {
            var resultsTextSb = new StringBuilder();
            var textToAdd     = String.Empty;

            if (!hideHeaders)
            {
                resultsTextSb.AppendLine(dbInfo.Database.Replace("__", "_") +
                                         "\t"                               +
                                         " ("                               +
                                         dbInfo.Server                      +
                                         ")"                                +
                                         Environment.NewLine                +
                                         PrintDashes(defaultDashesToShow));
            }

            foreach (var result in results)
            {
                if (hideHeaders && !String.IsNullOrEmpty(result.Header))
                {
                    resultsTextSb.AppendLine(isFirstResultRetrieved
                                                 ? result.
                                                     Rows // The header is removed here instead of the point of generation, since display order would be different from when the result was generated.
                                                 : result.Header + Environment.NewLine + result.Rows); // After the first result, remove header for subsequent results.

                    if (results.Count > 1)
                    {
                        resultsTextSb.AppendLine(); // If more than 1 query exists, add new line to differentiate, else combine all results.
                    }
                }
                else if (!hideHeaders)
                {
                    resultsTextSb.AppendLine(result.Header + Environment.NewLine + result.Rows + Environment.NewLine + Environment.NewLine);
                }
            }

            textToAdd = resultsTextSb.ToString();

            if (!isFirstResultRetrieved && !String.IsNullOrWhiteSpace(textToAdd))
            {
                isFirstResultRetrieved = true;
            }

            if (!String.IsNullOrWhiteSpace(textToAdd))
            {
                await SetResultTextboxContent(textToAdd);
            }
        }

        /// <summary>
        ///     Display the result to the user.
        /// </summary>
        /// <param name="dbInfo">The database info object.</param>
        /// <param name="dataSet">The resultant data set whose tables are to be displayed.</param>
        /// <returns>Returns a task to await the sending of the results to display.</returns>
        private async Task SendToDisplay(DbInfo dbInfo, DataSet dataSet)
        {
            SiteCounter++;
            Logger.Debug($"Setting result for {dbInfo.Server} / {dbInfo.Database}.");

            if (ResultDisplayType == ResultDisplayType.DifferentTabs)
            {
                // TODO: Pending Task.
                ////await SendResultToTabs(dbInfo, dataSet);
            }
            else
            {
                var results = GetResultText(dataSet, ResultDisplayType);

                if (results.Count > 0)
                {
                    if (ResultDisplayType == ResultDisplayType.Text || ResultDisplayType == ResultDisplayType.TextSqlFormatted)
                    {
                        await SendResultToText(results, dbInfo);
                    }
                    else if (ResultDisplayType == ResultDisplayType.TextFirstHeaderOnly)
                    {
                        await SendResultToText(results, dbInfo, true);
                    }
                    else if (ResultDisplayType == ResultDisplayType.DatabaseFileName)
                    {
                        await SendResultToIndividualFile(results, dbInfo);
                    }
                    else if (ResultDisplayType == ResultDisplayType.CombinedFile)
                    {
                        await SendResultToCombinedFile(results, dbInfo);
                    }
                }
            }

            ProgressText = String.Format("Completed {0} of {1}", SiteCounter, SitesToRun);
            QueryExecutionTimeText = DateTime.UtcNow.Subtract(queryExecutionStartDateTime).
                                              ToString(@"hh\:mm\:ss");

            if (DeselectOnQueryCompletion)
            {
                dbInfo.QueryExecutionRequested = false;
            }
        }

        /// <summary>
        ///     Set the content of the result text box.
        /// </summary>
        /// <param name="textToAdd">The content to add.</param>
        /// <returns>Task object that sets the result text box content.</returns>
        private async Task SetResultTextboxContent(String textToAdd)
        {
            // If the content to add is large, this process may take time. So await on it and let other progress bars/counters update and continue working.
            if (textToAdd.Length > 10000)
            {
                await Task.Run(() =>
                               {
                                   // Ensure that the threads wait here to prevent any possible inconsistencies in the display results due to race conditions that request currently displayed text.
                                   lock (lockObject)
                                   {
                                       var splitLength = 5000;
                                       var addedLength = 0;

                                       for (var splitIndex = 0; splitIndex < textToAdd.Length / splitLength; splitIndex++)
                                       {
                                           ResultsText += textToAdd.Substring(splitIndex * splitLength, splitLength);
                                           addedLength += splitLength;
                                           ////await Task.Delay(50); // Brief delay to let other progress bars/counters update and continue working.
                                       }

                                       ResultsText += textToAdd.Substring(addedLength, textToAdd.Length - addedLength);

                                       //TODO: Pending task
                                       ////TxtBlkResults.Visibility =  Visibility.Visible;
                                       ////TxtBlkResults.ScrollToEnd();
                                   }
                               });
            }
            else
            {
                //TODO: Pending task
                ResultsText += textToAdd;
                ////TxtBlkResults.Visibility =  Visibility.Visible;
                ////TxtBlkResults.ScrollToEnd();
            }
        }

        /// <summary>
        ///     Allow only numeric values.
        /// </summary>
        /// <param name="sender">The sender object.</param>
        /// <param name="e">The TextCompositionEventArgs object.</param>
        private void TxtConnTimeout_PreviewTextInput(Object sender, TextCompositionEventArgs e)
        {
            var regex = new Regex("[^0-9.-]+"); // Allow only 0-9
            e.Handled = regex.IsMatch(e.Text);
        }

        //TODO: Pending task.
        /////// <summary>
        ///////     Sets the connection timeout value.
        /////// </summary>
        /////// <param name="sender">The sender object.</param>
        /////// <param name="e">The RoutedEventArgs object.</param>
        ////private void TxtConnTimeout_TextChanged(Object sender, TextChangedEventArgs e)
        ////{
        ////    var conTimeout = 0;

        ////    if (Int32.TryParse(TxtConnTimeout.Text, out conTimeout))
        ////    {
        ////        ConnectionTimeout = conTimeout;
        ////    }
        ////    else
        ////    {
        ////        if (String.IsNullOrEmpty(TxtConnTimeout.Text))
        ////        {
        ////            TxtConnTimeout.Text = "30";
        ////        }
        ////        else
        ////        {
        ////            MessageBox.Show("Unable to set connection timeout.", MultiSqlSettings.ApplicationName);
        ////        }
        ////    }
        ////}

        /////// <summary>
        ///////     Capture key press on form.
        /////// </summary>
        /////// <param name="sender">The sender object.</param>
        /////// <param name="e">The KeyEventArgs object.</param>
        ////private void Window_PreviewKeyDown(Object sender, KeyEventArgs e)
        ////{
        ////    if (e.Key == Key.F5)
        ////    {
        ////        if (BtnRunQuery.Command != null && BtnRunQuery.Command.CanExecute(null))
        ////        {
        ////            BtnRunQuery.Command.Execute(null);
        ////        }
        ////    }
        ////    else if (e.Key == Key.F6)
        ////    {
        ////        if (BtnRunQuery.Command != null && BtnRunQuery.Command.CanExecute(null))
        ////        {
        ////            TxtQuery.Focus();
        ////            var selectionStart = TxtQuery.SelectionStart;
        ////            var selectionEnd   = TxtQuery.SelectionStart;

        ////            if (selectionStart == TxtQuery.Text.Length)
        ////            {
        ////                selectionStart = 0;
        ////            }

        ////            if (selectionEnd == TxtQuery.Text.Length)
        ////            {
        ////                selectionEnd--;
        ////            }

        ////            var canProcess      = false;
        ////            var processAttempts = 0;

        ////            while (!canProcess && processAttempts < 3)
        ////            {
        ////                while (selectionStart >= 0 && (TxtQuery.Text[selectionStart] != '\r' || selectionStart >= 2 && TxtQuery.Text[selectionStart - 2] != '\r'))
        ////                {
        ////                    if (selectionStart == 0)
        ////                    {
        ////                        break;
        ////                    }

        ////                    selectionStart--;
        ////                }

        ////                selectionStart -= 2;

        ////                if (selectionStart < 0)
        ////                {
        ////                    selectionStart = 0;
        ////                }

        ////                while (selectionEnd + 2 < TxtQuery.Text.Length && (TxtQuery.Text[selectionEnd] != '\r' || TxtQuery.Text[selectionEnd + 2] != '\r'))
        ////                {
        ////                    if (selectionEnd == TxtQuery.Text.Length - 1)
        ////                    {
        ////                        selectionEnd++;
        ////                        break;
        ////                    }

        ////                    selectionEnd++;
        ////                }

        ////                if (Math.Abs(selectionEnd - TxtQuery.Text.Length) <= 2)
        ////                {
        ////                    selectionEnd = TxtQuery.Text.Length;
        ////                }

        ////                selectionStart += selectionStart > 0 ? 4 : 0;
        ////                canProcess     =  selectionStart >= 0 && selectionEnd <= TxtQuery.Text.Length && selectionStart < selectionEnd;
        ////                processAttempts++;

        ////                if (!canProcess && selectionStart > 10)
        ////                {
        ////                    selectionStart -= 9;
        ////                }
        ////            }

        ////            if (canProcess)
        ////            {
        ////                TxtQuery.Select(selectionStart, selectionEnd - selectionStart);

        ////                if (TxtQuery.SelectedText.Replace(Environment.NewLine, String.Empty).Length > 0)
        ////                {
        ////                    BtnRunQuery.Command.Execute(null);
        ////                }
        ////            }
        ////        }
        ////    }
        ////}

        #endregion Private Methods

    }
}
