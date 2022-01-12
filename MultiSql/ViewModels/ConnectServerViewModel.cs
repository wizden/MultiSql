using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using MultiSql.Common;
using MultiSql.Models;
using NLog;

namespace MultiSql.ViewModels
{
    public class ConnectServerViewModel : ViewModelBase
    {

        #region Public Fields

        public ObservableCollection<ConnectionInfo> connectionInfos = new();

        #endregion Public Fields

        #region Private Fields

        private const String getDbListQuery =
            "SELECT name FROM sys.databases WHERE name NOT IN  ('ASPNETDB', 'ASPSTATE', 'master', 'tempdb', 'model', 'msdb', 'ReportServer', 'ReportServerTempDB') ORDER BY name;";

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly String                  SqlServerAuth = "SQL Server Authentication";
        private readonly String                  WindowsAuth   = "Windows Authentication";
        private          List<String>            _databases;
        private          String                  _errors;
        private          Int16                   _selectedConnectionIndex;
        private          List<String>            authenticationTypes;
        private          CancellationTokenSource cancellationTokenSource;
        private          RelayCommand            cmdCancel;
        private          RelayCommand            cmdConnect;
        private          Boolean                 connectionInProgress;
        private          XDocument               connectionListDocument;
        private          String                  selectedAuthenticationType;

        #endregion Private Fields

        #region Public Events

        public event EventHandler ConnectionChanged;

        #endregion Public Events

        #region Public Constructors

        public ConnectServerViewModel()
        {
            Logger.Debug("Opening Connection window.");
            SelectedAuthenticationType = WindowsAuth;
            Errors                     = String.Empty;
            cancellationTokenSource    = new CancellationTokenSource();

            // Not awaiting here as it's not critical to block user until all previous connections have been retrieved.
            _ = LoadConnectionsAsync();
        }

        #endregion Public Constructors

        #region Public Properties

        public List<String> AuthenticationTypes
        {
            get { return authenticationTypes ??= new List<String> {WindowsAuth, SqlServerAuth}; }
        }

        public RelayCommand CmdCancel
        {
            get { return cmdCancel ??= new RelayCommand(execute => CancelConnection(), canExecute => true); }
        }

        public RelayCommand CmdConnect
        {
            get { return cmdConnect ??= new RelayCommand(async execute => await ConnectToDbAsync(), canExecute => !connectionInProgress); }
        }

        public Boolean ConnectionCancelled { get; private set; }

        public SqlCredential ConnectionCredential { get; private set; }

        public ObservableCollection<ConnectionInfo> ConnectionInfos
        {
            get => connectionInfos;
            private set
            {
                connectionInfos = value;
                RaisePropertyChanged();
            }
        }

        public IEnumerable<String> ConnectionServers => ConnectionInfos.Select(ci => ci.ServerName);

        public ReadOnlyCollection<String> Databases => _databases?.AsReadOnly();

        public String Errors
        {
            get => _errors;
            set
            {
                _errors = value;
                RaisePropertyChanged();
            }
        }

        public SecureString Password { get; set; } = new();

        public String SelectedAuthenticationType
        {
            get => selectedAuthenticationType;

            set
            {
                selectedAuthenticationType = value;
                RaisePropertyChanged();
                RaisePropertyChanged("SqlAuthenticationRequested");
            }
        }

        public Int16 SelectedConnectionIndex
        {
            get => _selectedConnectionIndex;
            set
            {
                _selectedConnectionIndex = value;
                RaisePropertyChanged();

                if (value >= 0)
                {
                    var selectedConnection = ConnectionInfos.First(ci => ci.Id == value);
                    SelectedAuthenticationType = selectedConnection.IntegratedSecurity
                                                     ? WindowsAuth
                                                     : SqlServerAuth;
                    UserName = selectedConnection.UserName;
                    RaisePropertyChanged("UserName");
                }
            }
        }

        public String ServerConnectionString { get; private set; }
        public String ServerName             { get; set; }

        public Boolean SqlAuthenticationRequested => SelectedAuthenticationType == SqlServerAuth;

        public String UserName { get; set; }

        #endregion Public Properties

        #region Private Methods

        private void CancelConnection()
        {
            Logger.Debug("Cancelling the connection window.");
            cancellationTokenSource.Cancel();
            connectionInProgress = false;
            ConnectionCancelled  = true;
            ConnectionChanged?.Invoke(this, EventArgs.Empty);
        }

        private async Task ConnectToDbAsync()
        {
            connectionInProgress    = true;
            Errors                  = String.Empty;
            ConnectionCancelled     = false;
            _databases              = new List<String>();
            cancellationTokenSource = new CancellationTokenSource();
            var connString = new SqlConnectionStringBuilder();
            connString.ApplicationName          = MultiSqlSettings.ApplicationName;
            connString.DataSource               = ServerName;
            connString.IntegratedSecurity       = !SqlAuthenticationRequested;
            connString.MultipleActiveResultSets = true;

            if (SqlAuthenticationRequested)
            {
                Password.MakeReadOnly();
                ConnectionCredential = new SqlCredential(UserName, Password);
                Password             = new SecureString();
            }

            try
            {
                Logger.Debug($"Attempting connection. Server: {ServerName}, Integrated Security: {connString.IntegratedSecurity}, User: {ConnectionCredential?.UserId ?? String.Empty}.");
                using var conn = new SqlConnection(connString.ConnectionString);
                using var cmd  = new SqlCommand(getDbListQuery, conn);
                conn.Credential = ConnectionCredential;
                await conn.OpenAsync(cancellationTokenSource.Token);
                using var sdr = await cmd.ExecuteReaderAsync(cancellationTokenSource.Token);

                if (sdr.HasRows)
                {
                    Logger.Debug("Connection succeeded.");

                    while (sdr.Read())
                    {
                        _databases.Add(await sdr.GetFieldValueAsync<String>(0, cancellationTokenSource.Token));
                    }
                }


                conn.Close();
                connectionInProgress   = false;
                UserName               = ConnectionCredential?.UserId ?? String.Empty;
                ServerConnectionString = connString.ConnectionString;
                ConnectionChanged?.Invoke(this, EventArgs.Empty);

                // Not awaiting here as it's not critical that the save should occur to block the user from connecting.
                _ = SaveConnectionToListAsync(connString.DataSource, connString.IntegratedSecurity);
            }
            catch (Exception exception)
            {
                Errors = exception.Message;
                Logger.Error(exception);
            }
            finally
            {
                connectionInProgress = false;
            }
        }

        private void CreateConnectionDocument()
        {
            Logger.Debug($"No connections file found. Creating new file in {MultiSqlSettings.ConnectionsListFile}.");
            var root = new XElement("Connections", new XElement[] {null});
            connectionListDocument = XDocument.Parse(root.ToString(), LoadOptions.None);
            connectionListDocument.Save(MultiSqlSettings.ConnectionsListFile);
        }

        private async Task LoadConnectionsAsync()
        {
            await Task.Run(() =>
                           {

                               try
                               {
                                   Logger.Debug("Retrieving connections list file.");

                                   if (!File.Exists(MultiSqlSettings.ConnectionsListFile))
                                   {
                                       CreateConnectionDocument();
                                       return;
                                   }

                                   var connectionContent = File.ReadAllText(MultiSqlSettings.ConnectionsListFile);

                                   if (String.IsNullOrWhiteSpace(connectionContent))
                                   {
                                       CreateConnectionDocument();
                                       return;
                                   }

                                   connectionListDocument = XDocument.Parse(connectionContent);
                                   ConnectionInfos        = new ObservableCollection<ConnectionInfo>();
                                   Int16 id = 0;

                                   foreach (var conInfo in connectionListDocument.Descendants("Connection").
                                                                                  OrderByDescending(d => DateTime.Parse(d.Attribute("LastUsed").Value)))
                                   {
                                       ConnectionInfos.Add(new ConnectionInfo(id++,
                                                                              conInfo.Attribute("Server").Value,
                                                                              Boolean.Parse(conInfo.Attribute("IntegratedSecurity").Value),
                                                                              conInfo.Attribute("UserName").Value,
                                                                              DateTime.Parse(conInfo.Attribute("LastUsed").Value)));
                                   }
                               }
                               catch (XmlException)
                               {
                                   Logger.Error($"Unable to parse XML from document in {MultiSqlSettings.ConnectionsListFile}. If the file is corrupt, feel free to delete it and the program will generate a new one for future use.");
                               }
                               catch (FileNotFoundException)
                               {
                                   CreateConnectionDocument();
                               }
                           });
        }

        private async Task SaveConnectionToListAsync(String serverName, Boolean integratedSecurity)
        {
            await Task.Run(() =>
                           {
                               ConnectionCredential = null;
                               Logger.Debug($"Retrieving connection information for Server: {serverName}, Integrated Security: {integratedSecurity}, User: {UserName}");

                               var conn = connectionListDocument.Descendants("Connection").
                                                                 FirstOrDefault(con =>
                                                                                    String.Compare(con.Attribute("Server")?.Value,
                                                                                                   serverName,
                                                                                                   StringComparison.CurrentCultureIgnoreCase) ==
                                                                                    0 &&
                                                                                    String.Compare(con.Attribute("UserName")?.Value,
                                                                                                   UserName,
                                                                                                   StringComparison.CurrentCultureIgnoreCase) ==
                                                                                    0 &&
                                                                                    String.Compare(con.Attribute("IntegratedSecurity")?.Value,
                                                                                                   integratedSecurity.ToString(),
                                                                                                   StringComparison.CurrentCultureIgnoreCase) ==
                                                                                    0);

                               if (conn == null)
                               {
                                   Logger.Debug("No connection information found. Adding details of new connection.");
                                   connectionListDocument.Descendants("Connections").
                                                          FirstOrDefault().
                                                          Add(new XElement("Connection",
                                                                           new XAttribute("Server",             serverName),
                                                                           new XAttribute("UserName",           UserName ?? String.Empty),
                                                                           new XAttribute("IntegratedSecurity", integratedSecurity),
                                                                           new XAttribute("LastUsed",           DateTime.Now.ToString())));
                               }
                               else
                               {
                                   Logger.Debug("Updating last used date/time of connection.");
                                   conn.Attribute("LastUsed").Value = DateTime.Now.ToString();
                               }

                               connectionListDocument.Save(MultiSqlSettings.ConnectionsListFile);
                           });
        }

        #endregion Private Methods

    }
}
