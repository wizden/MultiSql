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

namespace MultiSql.UserControls.ViewModels
{
    internal class ConnectServerViewModel : ViewModelBase
    {

        #region Public Fields

        public List<ConnectionInfo> connectionInfos = new();

        #endregion Public Fields

        #region Private Fields

        private const String getDbListQuery =
            "SELECT name FROM sys.databases WHERE name NOT IN  ('ASPNETDB', 'ASPSTATE', 'master', 'tempdb', 'model', 'msdb', 'ReportServer', 'ReportServerTempDB') ORDER BY name;";

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly String                  SqlServerAuth = "SQL Server Authentication";
        private readonly String                  WindowsAuth   = "Windows Authentication";
        private          List<String>            _databases;
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
            SelectedViewModel          = this;
            SelectedAuthenticationType = WindowsAuth;
            LoadConnectionsAsync();
        }

        #endregion Public Constructors

        #region Public Properties

        private ViewModelBase _selectedViewModel;

        public List<String> AuthenticationTypes
        {
            get { return authenticationTypes ??= new List<String> {WindowsAuth, SqlServerAuth}; }
        }

        public RelayCommand CmdCancel
        {
            get { return cmdCancel ??= new RelayCommand(async execute => await CancelConnection(), canExecute => connectionInProgress); }
        }

        public RelayCommand CmdConnect
        {
            get { return cmdConnect ??= new RelayCommand(async execute => await ConnectToDbAsync(), canExecute => !connectionInProgress); }
        }

        public List<ConnectionInfo> ConnectionInfos
        {
            get => connectionInfos;
            private set
            {
                connectionInfos = value;
                RaisePropertyChanged();
            }
        }

        public ReadOnlyCollection<String> Databases => _databases?.AsReadOnly();

        public String Errors { get; set; }

        public SecureString Password { get; set; }

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

        public ViewModelBase SelectedViewModel
        {
            get => _selectedViewModel;
            set
            {
                _selectedViewModel = value;
                RaisePropertyChanged();
            }
        }

        public String ServerConnectionString { get; private set; }
        public String ServerName             { get; set; }

        public Boolean SqlAuthenticationRequested => SelectedAuthenticationType == SqlServerAuth;
        public String  UserName                   { get; set; }

        #endregion Public Properties

        #region Private Methods

        private async Task CancelConnection()
        {
            Logger.Debug("Cancelling the connection window.");

            if (connectionInProgress)
            {
                cancellationTokenSource.Cancel();
                connectionInProgress = false;
            }
        }

        private async Task ConnectToDbAsync()
        {
            connectionInProgress    = true;
            cancellationTokenSource = new CancellationTokenSource();
            SetErrorText(String.Empty);
            _databases = new List<String>();
            var connString = new SqlConnectionStringBuilder();
            connString.DataSource               = ServerName;
            connString.IntegratedSecurity       = !SqlAuthenticationRequested;
            connString.MultipleActiveResultSets = true;
            SqlCredential credential = null;

            if (SqlAuthenticationRequested)
            {
                Password.MakeReadOnly();
                credential = new SqlCredential(UserName, Password);
            }

            try
            {
                Logger.Debug($"Attempting connection. Server: {ServerName}, Integrated Security: {connString.IntegratedSecurity}, User: {UserName}.");

                using var conn = new SqlConnection(connString.ConnectionString);
                using var cmd  = new SqlCommand(getDbListQuery, conn);
                conn.Credential = credential;
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

                // Not awaiting here as it's not critical that the save should occur to block the user from connecting.
                SaveConnectionToListAsync(connString.DataSource, connString.UserID, connString.IntegratedSecurity);
                conn.Close();
                connectionInProgress   = false;
                ServerConnectionString = conn.ConnectionString;
                ConnectionChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception exception)
            {
                SetErrorText(exception.Message);
                Logger.Error(exception);
            }
            finally
            {
                connectionInProgress = false;
            }
        }

        private async Task LoadConnectionsAsync()
        {
            await Task.Run(() =>
                           {

                               try
                               {
                                   Logger.Debug("Retrieving connections list file.");
                                   connectionListDocument = XDocument.Parse(File.ReadAllText(MultiSqlSettings.ConnectionsListFile));
                                   ConnectionInfos        = new List<ConnectionInfo>();

                                   foreach (var conInfo in connectionListDocument.Descendants("Connection"))
                                   {
                                       ConnectionInfos.Add(new ConnectionInfo(conInfo.Attribute("Server").Value,
                                                                              Boolean.Parse(conInfo.Attribute("IntegratedSecurity").Value),
                                                                              conInfo.Attribute("UserName").Value,
                                                                              DateTime.Parse(conInfo.Attribute("LastUsed").Value)));
                                   }

                                   ConnectionInfos = ConnectionInfos.OrderByDescending(ci => ci.LastUsedDateTime).ToList();
                               }
                               catch (XmlException xe)
                               {
                                   Logger.Error($"Unable to parse XML from document in {MultiSqlSettings.ConnectionsListFile}. If the file is corrupt, feel free to delete it and the program will generate a new one for future use.");
                               }
                               catch (FileNotFoundException ffe)
                               {
                                   Logger.Debug($"No connections file found. Creating new file in {MultiSqlSettings.ConnectionsListFile}.");
                                   var root = new XElement("Connections", new XElement[] {null});
                                   connectionListDocument = XDocument.Parse(root.ToString(), LoadOptions.None);
                                   connectionListDocument.Save(MultiSqlSettings.ConnectionsListFile);
                               }
                           });

            var lastConnectionInfo = ConnectionInfos.FirstOrDefault() ?? new ConnectionInfo(String.Empty, true, String.Empty, DateTime.Now);
            ////CmbServerName.Text = lastConnectionInfo.ServerName;
            ////CmbAuthenticationType.Text = lastConnectionInfo.IntegratedSecurity ? WindowsAuth : SqlServerAuth;
            ////TxtUserName.Text = lastConnectionInfo.UserName;
        }

        private async Task SaveConnectionToListAsync(String serverName, String userName, Boolean integratedSecurity)
        {
            await Task.Run(() =>
                           {
                               Logger.Debug($"Retrieving connection information for Server: {serverName}, Integrated Security: {integratedSecurity}, User: {userName}");
                               var conn = connectionListDocument.Descendants("Connection").
                                                                 FirstOrDefault(con =>
                                                                                    String.Compare(con.Attribute("Server")?.Value,
                                                                                                   serverName,
                                                                                                   StringComparison.CurrentCultureIgnoreCase) ==
                                                                                    0 &&
                                                                                    String.Compare(con.Attribute("UserName")?.Value,
                                                                                                   userName,
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
                                                                           new XAttribute("UserName",           userName),
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

        private void SetErrorText(String errorText)
        {
            Errors = errorText;
        }

        #endregion Private Methods

    }
}
