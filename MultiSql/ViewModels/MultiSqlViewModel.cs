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
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Input;
using System.Xml;
using System.Xml.Schema;
using MultiSql.Common;
using MultiSql.Models;
using NLog;
using static MultiSql.Common.MultiSqlSettings;
using MessageBox = System.Windows.MessageBox;
using MessageBoxOptions = System.Windows.MessageBoxOptions;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
using Timer = System.Timers.Timer;

namespace MultiSql.ViewModels
{
    public class MultiSqlViewModel : ViewModelBase
    {

        /* TODO:
        Software updates

        */

        #region Public Constructors

        /// <summary>
        ///     Initialises a new instance of the <see cref="MultiSqlViewModel" /> class.
        /// </summary>
        public MultiSqlViewModel()
        {
            DatabaseListViewModel  =  new DbCheckedListViewModel();
            executionTimer         =  new Timer(1000);
            executionTimer.Elapsed += ExecutionTimer_Elapsed;
            DatabaseListExpanded   =  true;

            // TODO: Remove at commit
            QueryAllText      = "SELECT TOP 10 * FROM PAYRESULTSMISC";
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
        ///     Private store for the connection timeout.
        /// </summary>
        private Int32 _connectionTimeout = 30;

        /// <summary>
        ///     Private store to indicate whether the database list is expanded.
        /// </summary>
        private Boolean _databaseListExpanded;

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
        ///     Private store for the collection of tab items on running queries when the ResultType is set to "Tabs".
        /// </summary>
        private ObservableCollection<ITabItem> _tabItems;

        /// <summary>
        ///     Private store for the cancellation token source object.
        /// </summary>
        private CancellationTokenSource cancellationTokenSource;

        /// <summary>
        ///     Private variable to hold the command object to cancel the running query.
        /// </summary>
        private RelayCommand cancelQueryCommand;

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
        public ObservableCollection<DatabaseViewModel> AllDatabases => DatabaseListViewModel.AllDatabases;

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
            get => _connectionTimeout;

            set
            {
                _connectionTimeout = value == 0 ? 30 : value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        ///     Gets the view model for the database list user control.
        /// </summary>
        public DbCheckedListViewModel DatabaseListViewModel { get; }

        /// <summary>
        ///     Gets or sets a value indicating whether the database list is expanded.
        /// </summary>
        public Boolean DatabaseListExpanded
        {
            get => _databaseListExpanded;
            set
            {
                _databaseListExpanded = value;
                DatabasesTextDisplay  = "Databases" + (value ? String.Empty : $" ({DatabaseListViewModel.GetDatabasesSelectedCountText})");
                RaisePropertyChanged();
                RaisePropertyChanged("DatabasesTextDisplay");
            }
        }

        /// <summary>
        ///     Gets or set the text to be displayed for databases count if the control is collapsed.
        /// </summary>
        public String DatabasesTextDisplay { get; set; }

        /// <summary>
        ///     Gets or sets the default selected index.
        /// </summary>
        public Int32 DefaultSelectedIndex { get; set; }

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
                           ? String.Format("{0} of {1}", AllDatabases.Count(dh => dh.IsChecked).ToString(), AllDatabases.Count().ToString())
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

        /// <summary>
        ///     Gets or sets the collection of tab items on running queries when the ResultType is set to "Tabs".
        /// </summary>
        public ObservableCollection<ITabItem> TabItems
        {
            get => _tabItems;
            set
            {
                _tabItems = value;
                RaisePropertyChanged();
            }
        }

        #endregion Public Properties

        #region Private Methods

        /// <summary>
        ///     Add error information to the errors text box.
        /// </summary>
        /// <param name="message">The message to be added.</param>
        private void AddToErrorText(String message)
        {
            if (String.IsNullOrWhiteSpace(message))
            {
                Errors = String.Empty;
            }
            else
            {
                Errors += message + "\r\n\r\n";
                Logger.Error(message);
            }
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
            return !String.IsNullOrEmpty(QueryAllText) && !isQueryRunning && (AllDatabases?.Any(dh => dh.IsChecked) ?? false);
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
            List<DatabaseViewModel> databasesToRun = new();
            AddToErrorText(String.Empty);
            queryToExecute               = String.Join(Environment.NewLine, String.Join(Environment.NewLine + Environment.NewLine, QueriesToExecute));
            databasesToRun               = AllDatabases.Where(dh => dh.IsChecked).ToList();
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
                        //TODO: Find way to remove MessageBox.Show
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
        private async Task<Boolean> RunQueryOnDatabase(DatabaseViewModel dbInfo)
        {
            DataSet individualQueryResult = null;
            var associatedConnectionStringBuilder = DatabaseListViewModel.ServerList.
                                                                          FirstOrDefault(svm => svm.Databases.Contains(dbInfo)).
                                                                          ConnectionStringBuilder;

            associatedConnectionStringBuilder.InitialCatalog = dbInfo.DatabaseName.Replace("__", "_");
            associatedConnectionStringBuilder.ConnectTimeout = ConnectionTimeout;

            using (var con = new SqlConnection(associatedConnectionStringBuilder.ConnectionString))
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
                                                 dbInfo.Database.ServerName,
                                                 dbInfo.DatabaseName.Replace("__", "_"),
                                                 errorText,
                                                 PrintDashes(defaultDashesToShow)));
                }
                catch (SqlException sqlEx)
                {
                    if (!sqlEx.Message.Contains("Operation cancelled by user."))
                    {
                        AddToErrorText(String.Format("{0}.{1}\r\n{2}\r\n{3}",
                                                     dbInfo.Database.ServerName,
                                                     dbInfo.DatabaseName.Replace("__", "_"),
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
        private async Task RunQueryOnDatabasesAsync(List<DatabaseViewModel> dbInfos)
        {
            Logger.Debug("Preparing to run query on databases.");
            databaseQueries         = new List<Task>();
            cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Token.ThrowIfCancellationRequested();

            ResultsText            = String.Empty;
            ProgressText           = String.Format("Completed {0} of {1}", SiteCounter, SitesToRun);
            fileSaveLocation       = String.Empty;
            QueryExecutionTimeText = String.Empty;

            if (ResultDisplayType == ResultDisplayType.DifferentTabs)
            {
                TabItems = new ObservableCollection<ITabItem>();
                BindingOperations.EnableCollectionSynchronization(TabItems, lockObject);
            }

            foreach (var dh in dbInfos.OrderBy(dh => dh.Database.ServerName).ThenBy(dh => dh.DatabaseName).ToList())
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

                ProgressText = String.Empty;
                Logger.Debug("Completed running query on databases.");

                if (ResultDisplayType == ResultDisplayType.DifferentTabs && TabItems.Count > 0)
                {
                    RaisePropertyChanged("DefaultSelectedIndex");
                }
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
        private async Task SendResultToCombinedFile(List<Result> results, DatabaseViewModel dbInfo = null)
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
        private async Task SendResultToIndividualFile(List<Result> results, DatabaseViewModel dbInfo = null)
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
                        File.WriteAllText(Path.Combine(fileSaveLocation, dbInfo.DatabaseName.Replace("__", "_") + ".txt"), fileContent.ToString());
                    }
                }
            }
        }

        /// <summary>
        ///     Display the result on separate tabs.
        /// </summary>
        /// <param name="dbInfo">The database object.</param>
        /// <param name="dataSet">The resultant data set whose tables are to be displayed.</param>
        private async Task SendResultToTabs(DatabaseViewModel dbInfo, DataSet dataSet)
        {
            var totalRows = 0;

            if (dataSet != null && dataSet.Tables.Count > 0)
            {
                await Task.Run(() =>
                               {
                                   foreach (DataTable dataTable in dataSet.Tables)
                                   {
                                       totalRows += dataTable.Rows.Count;
                                   }

                                   if (!(IgnoreEmptyResults && totalRows == 0))
                                   {
                                       var databaseResultsTabItem = new DatabaseResultsTabItemViewModel(dbInfo.DatabaseName, dataSet);
                                       databaseResultsTabItem.ResultTableSelected += DatabaseResultsTabItem_ResultTableSelected;
                                       TabItems.Add(databaseResultsTabItem);
                                   }
                               });
            }
        }

        /// <summary>
        ///     Set the row count on the display.
        /// </summary>
        /// <param name="sender">The parameter is not used.</param>
        /// <param name="e">Arguments containing the row count for the table.</param>
        private void DatabaseResultsTabItem_ResultTableSelected(Object sender, ResultTableSelectedEventArgs e)
        {
            ProgressText = $"Rows: {e.RowCount}";
        }

        /// <summary>
        ///     Display the result on separate tabs.
        /// </summary>
        /// <param name="results">The result of the query.</param>
        /// <param name="dbInfo">The database object.</param>
        /// <param name="hideHeaders">Hide connection header information.</param>
        private async Task SendResultToText(List<Result> results, DatabaseViewModel dbInfo, Boolean hideHeaders = false)
        {
            var resultsTextSb = new StringBuilder();
            var textToAdd     = String.Empty;

            if (!hideHeaders)
            {
                resultsTextSb.AppendLine(dbInfo.DatabaseName.Replace("__", "_") +
                                         "\t"                                   +
                                         " ("                                   +
                                         dbInfo.Database.ServerName             +
                                         ")"                                    +
                                         Environment.NewLine                    +
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
        private async Task SendToDisplay(DatabaseViewModel dbInfo, DataSet dataSet)
        {
            SiteCounter++;
            Logger.Debug($"Setting result for {dbInfo.Database.ServerName} / {dbInfo.DatabaseName}.");

            if (ResultDisplayType == ResultDisplayType.DifferentTabs)
            {
                await SendResultToTabs(dbInfo, dataSet);
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
                dbInfo.IsChecked = false;
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
                                       }

                                       ResultsText += textToAdd.Substring(addedLength, textToAdd.Length - addedLength);
                                   }
                               });
            }
            else
            {
                ResultsText += textToAdd;
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

        #endregion Private Methods

    }
}
