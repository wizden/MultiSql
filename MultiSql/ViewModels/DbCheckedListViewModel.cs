using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Xml;
using System.Xml.Schema;
using Microsoft.Win32;
using MultiSql.Common;

namespace MultiSql.ViewModels
{
    /// <summary>
    ///     View model for the database checked list user control.
    /// </summary>
    public class DbCheckedListViewModel : ViewModelBase
    {

        public event EventHandler ChangeConnection;

        public static Int16 id;

        #region Private Fields

        /// <summary>
        ///     Private store for the command to change the server connection.
        /// </summary>
        private RelayCommand _cmdChangeConnection;

        /// <summary>
        ///     Private store for the command to load the database list.
        /// </summary>
        private RelayCommand _cmdLoadList;

        /// <summary>
        ///     Private store for the command to save the database list.
        /// </summary>
        private RelayCommand _cmdSaveList;

        /// <summary>
        ///     Private store for the database filter text.
        /// </summary>
        private String _databaseFilterText = String.Empty;

        /// <summary>
        ///     Private store for the list of databases.
        /// </summary>
        private TrulyObservableCollection<DatabaseViewModel> _allDatabases = new();

        /// <summary>
        ///     Private store for the error text.
        /// </summary>
        private String errorText = String.Empty;

        /// <summary>
        ///     Private store to determine if the query is running.
        /// </summary>
        private Boolean isQueryRunning;

        /// <summary>
        ///     Private store determining whether all the databases are to be selected.
        /// </summary>
        private Boolean selectAllDatabases;

        /// <summary>
        ///     Private store for the list of server view models.
        /// </summary>
        private TrulyObservableCollection<ServerViewModel> _serverList = new();
        
        #endregion Private Fields

        #region Public Constructors

        /// <summary>
        ///     Initialises a new instance of the <see cref="DbCheckedListViewModel" /> class.
        /// </summary>
        public DbCheckedListViewModel() { }

        #endregion Public Constructors

        #region Public Properties

        public ObservableCollection<ServerViewModel> ServerList
        {
            get => _serverList;

            private set
            {
                foreach (var serverViewModel in value)
                {
                    _serverList.Add(serverViewModel);
                }

                RaisePropertyChanged();
                RaisePropertyChanged("AllDatabases");
                RaisePropertyChanged("ConnectionExists");
            }
        }

        /// <summary>
        ///     Gets the list of databases for the checked list control.
        /// </summary>
        public ObservableCollection<DatabaseViewModel> AllDatabases
        {
            get
            {
                if (_allDatabases == null || _allDatabases.Count != ServerList.SelectMany(sl => sl.Databases).Count())
                {
                    _allDatabases = new TrulyObservableCollection<DatabaseViewModel>();

                    foreach (var databaseViewModel in ServerList.SelectMany(sl => sl.Databases))
                    {
                        databaseViewModel.QueryExecutionRequestedChanged += DbInfo_QueryExecutionRequestedChanged;
                        _allDatabases.Add(databaseViewModel);
                    }

                    RaisePropertyChanged();
                    RaisePropertyChanged("ConnectionExists");
                }

                return _allDatabases;
            }
        }

        /// <summary>
        ///     Command to change the server connection.
        /// </summary>
        public RelayCommand CmdChangeConnection
        {
            get { return _cmdChangeConnection ??= new RelayCommand(execute => ChangeSQLConnection(), canExecute => !isQueryRunning); }
        }

        /// <summary>
        ///     Command to load the list of databases for selection.
        /// </summary>
        public RelayCommand CmdLoadList
        {
            get { return _cmdLoadList ??= new RelayCommand(async execute => await LoadList(), canExecute => AllDatabases?.Count > 0); }
        }

        /// <summary>
        ///     Command to save the list of databases for future selection.
        /// </summary>
        public RelayCommand CmdSaveList
        {
            get { return _cmdSaveList ??= new RelayCommand(async execute => await SaveList(), canExecute => AllDatabases?.Any(db => db.IsChecked) ?? false); }
        }

        public Boolean ConnectionExists => AllDatabases?.Count > 0;

        /// <summary>
        ///     Gets the connection string builder for the server connection.
        /// </summary>
        public SqlConnectionStringBuilder ConnectionStringBuilder { get; set; }

        /// <summary>
        ///     Gets or sets the text to filter the database names.
        /// </summary>
        public String DatabaseFilterText
        {
            get => _databaseFilterText;

            set
            {
                _databaseFilterText                                                         = value;
                ((CollectionView) CollectionViewSource.GetDefaultView(AllDatabases)).Filter = DatabaseFilter;
            }
        }

        /// <summary>
        ///     Gets or sets any error text associated with the control.
        /// </summary>
        public String ErrorText
        {
            get => errorText;

            private set
            {
                errorText = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        ///     Gets the number of databases selected.
        /// </summary>
        public String GetDatabasesSelectedCountText
        {
            get { return String.Format("Selected {0} of {1}", AllDatabases.Where(dh => dh.IsChecked).Count().ToString(), AllDatabases.Count().ToString()); }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether the query is running.
        /// </summary>
        public Boolean IsQueryRunning
        {
            get => isQueryRunning;

            set
            {
                isQueryRunning = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether all the databases are selected.
        /// </summary>
        public Boolean SelectAllDatabases
        {
            get => selectAllDatabases;

            set
            {
                selectAllDatabases = value;
                AllDatabases.
                    Where(dh => dh.DatabaseName.ToUpper().Contains(DatabaseFilterText.ToUpper()) || dh.Database.ServerName.ToUpper().Contains(DatabaseFilterText.ToUpper())).
                    ToList().
                    ForEach(dh => dh.IsChecked = value);
                RaisePropertyChanged("GetDatabasesSelectedCountText");
                RaisePropertyChanged();
            }
        }

        #endregion Public Properties

        public String AddServer(ConnectServerViewModel connectServer)
        {
            var retVal = String.Empty;

            try
            {
                if (connectServer.ConnectionCancelled)
                {
                    return retVal;
                }

                if (ServerList.Any(svm => svm.ServerName         == connectServer.ServerName                 &&
                                          svm.IntegratedSecurity != connectServer.SqlAuthenticationRequested &&
                                          svm.UserName           == connectServer.UserName))
                {
                    retVal = "Server already connected with those credentials.";
                    return retVal;
                }

                var serverViewModel = new ServerViewModel(new SqlConnectionStringBuilder(connectServer.ServerConnectionString), connectServer.ConnectionCredential);

                foreach (var database in connectServer.Databases)
                {
                    serverViewModel.Databases.Add(new DatabaseViewModel(id++,
                                                                        serverViewModel,
                                                                        database,
                                                                        !connectServer.SqlAuthenticationRequested,
                                                                        connectServer.UserName,
                                                                        DateTime.Now));
                }

                ServerList.Add(serverViewModel);
                RaisePropertyChanged("ServerList");
            }
            catch (Exception e)
            {
                retVal = e.Message;
            }

            return retVal;
        }

        #region Private Methods

        /// <summary>
        ///     Change the connection for the control.
        /// </summary>
        private void ChangeSQLConnection()
        {
            ChangeConnection?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        ///     Sets up the database filter.
        /// </summary>
        /// <param name="item">The filter item text.</param>
        /// <returns>Boolean indicating the list of databases based on the filter.</returns>
        private Boolean DatabaseFilter(Object item)
        {
            if (String.IsNullOrEmpty(DatabaseFilterText))
            {
                return true;
            }

            if (item is DatabaseViewModel)
            {
                var dbObject = (DatabaseViewModel) item;
                var retVal   = dbObject.DatabaseName.Replace("__", "_").ToUpper().Contains(DatabaseFilterText.ToUpper());
                return retVal;
            }

            return false;
        }

        private void DbInfo_QueryExecutionRequestedChanged(Object sender, EventArgs e)
        {
            RaisePropertyChanged("GetDatabasesSelectedCountText");
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

        private async Task LoadList()
        {
            var ofd = new OpenFileDialog {Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*", CheckPathExists = true, CheckFileExists = true, Multiselect = false};

            await Task.Run(() =>
                           {
                               Application.Current.Dispatcher.Invoke(() =>
                                                                     {
                                                                         if (ofd.ShowDialog().Value)
                                                                         {
                                                                             ErrorText = String.Empty;

                                                                             try
                                                                             {
                                                                                 var databasesNotFound = new List<String>();
                                                                                 var databaseList      = File.ReadAllLines(ofd.FileName).Select(s => s.ToUpper());

                                                                                 foreach (var databaseName in databaseList)
                                                                                 {
                                                                                     try
                                                                                     {
                                                                                         AllDatabases.First(db => db.DatabaseName.ToUpper() == databaseName).
                                                                                                      IsChecked = !SelectAllDatabases;
                                                                                     }
                                                                                     catch (InvalidOperationException ioe)
                                                                                     {
                                                                                         databasesNotFound.Add(databaseName);
                                                                                     }
                                                                                 }

                                                                                 if (databasesNotFound.Count > 0)
                                                                                 {
                                                                                     ErrorText = "Unable to match databases from list: " +
                                                                                                 String.Join(", ", databasesNotFound)    +
                                                                                                 ".";
                                                                                 }
                                                                             }
                                                                             catch (IOException iex)
                                                                             {
                                                                                 ErrorText = iex.Message;
                                                                             }
                                                                         }
                                                                     });
                           });


        }

        private async Task SaveList()
        {
            var sfd = new SaveFileDialog();
            sfd.Filter          = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
            sfd.CheckPathExists = true;
            ErrorText           = String.Empty;

            await Task.Run(() =>
                           {
                               Application.Current.Dispatcher.Invoke(() =>
                                                                     {
                                                                         if (sfd.ShowDialog().Value)
                                                                         {
                                                                             try
                                                                             {
                                                                                 var dbList = String.Join(Environment.NewLine,
                                                                                                          AllDatabases.Where(db => db.IsChecked).
                                                                                                                       Select(db => db.Database));

                                                                                 File.WriteAllText(sfd.FileName, dbList);
                                                                             }
                                                                             catch (IOException iex)
                                                                             {
                                                                                 ErrorText = iex.Message;
                                                                             }
                                                                         }
                                                                     });
                           });

        }

        #endregion Private Methods

    }
}
