using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Xml;
using System.Xml.Linq;
using MultiSql.Common;
using MultiSql.Models;
using NLog;

namespace MultiSql
{
    /// <summary>
    /// Interaction logic for ConnectServer.xaml
    /// </summary>
    public partial class ConnectServer : Window
    {

        // TODO: Change to allow loading of connections into controls on screen.

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly String WindowsAuth = "Windows Authentication";

        private readonly String SqlServerAuth = "SQL Server Authentication";

        private const String getDbListQuery =
            "SELECT name FROM sys.databases WHERE name NOT IN  ('ASPNETDB', 'ASPSTATE', 'master', 'tempdb', 'model', 'msdb', 'ReportServer', 'ReportServerTempDB') ORDER BY name;";

        private CancellationTokenSource cancellationTokenSource;

        private XDocument connectionListDocument;

        public List<ConnectionInfo> ConnectionInfos { get; private set; }

        private List<String> _databases;

        private Boolean connectionInProgress;

        public ReadOnlyCollection<String> Databases => _databases?.AsReadOnly();

        public String ServerConnectionString { get; private set; }

        public ConnectServer()
        {
            Logger.Debug("Opening Connection window.");
            LoadConnectionsAsync();
            InitializeComponent();
            CmbAuthenticationType.Items.Add(WindowsAuth);
            CmbAuthenticationType.Items.Add(SqlServerAuth);
            CmbAuthenticationType.SelectedIndex = 0;
            CmbServerName.Focus();
        }

        private void BtnCancel_OnClick(Object sender, RoutedEventArgs e)
        {
            Logger.Debug("Cancelling the connection window.");

            if (connectionInProgress)
            {
                cancellationTokenSource.Cancel();
                BtnConnect.IsEnabled = true;
            }
            else
            {
                Close();
            }
        }

        private async void BtnConnect_OnClick(Object sender, RoutedEventArgs e)
        {
            connectionInProgress    = true;
            cancellationTokenSource = new CancellationTokenSource();
            SetErrorText(String.Empty);
            BtnConnect.IsEnabled = false;
            _databases           = new List<String>();
            var connString = new SqlConnectionStringBuilder();
            connString.DataSource               = CmbServerName.Text;
            connString.IntegratedSecurity       = !TxtUserName.IsEnabled;
            connString.MultipleActiveResultSets = true;

            if (TxtUserName.IsEnabled)
            {
                connString.UserID   = TxtUserName.IsEnabled ? TxtUserName.Text : String.Empty;
                connString.Password = TxtPassword.IsEnabled ? TxtPassword.Password : String.Empty;
            }

            try
            {
                Logger.Debug($"Attempting connection. Server: {CmbServerName.Text}, Integrated Security: {connString.IntegratedSecurity.ToString()}, User: {TxtUserName.Text}.");

                using var conn = new SqlConnection(connString.ConnectionString);
                using var cmd  = new SqlCommand(getDbListQuery, conn);
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
                Close();
            }
            catch (Exception exception)
            {
                SetErrorText(exception.Message);
                Height += 50;
                Logger.Error(exception);
            }
            finally
            {
                BtnConnect.IsEnabled = true;
                connectionInProgress = false;
            }
        }

        private void CmbAuthenticationType_OnSelectionChanged(Object sender, SelectionChangedEventArgs e)
        {
            var cmbAuth = sender as ComboBox;

            if (cmbAuth != null)
            {
                TxtUserName.IsEnabled = cmbAuth.SelectedItem.ToString() == SqlServerAuth;
                TxtPassword.IsEnabled = cmbAuth.SelectedItem.ToString() == SqlServerAuth;
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
            CmbServerName.Text         = lastConnectionInfo.ServerName;
            CmbAuthenticationType.Text = lastConnectionInfo.IntegratedSecurity ? WindowsAuth : SqlServerAuth;
            TxtUserName.Text           = lastConnectionInfo.UserName;
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
            TxtErrors.Text       = errorText;
            TxtErrors.Visibility = String.IsNullOrWhiteSpace(errorText) ? Visibility.Collapsed : Visibility.Visible;
            Height               = MinHeight + (String.IsNullOrWhiteSpace(errorText) ? 0 : 50);
        }

        private void ConnectServer_OnClosing(Object sender, CancelEventArgs e)
        {
            if (connectionInProgress)
            {
                e.Cancel = true;
                cancellationTokenSource.Cancel();
            }

            Logger.Debug("Closing connection window.");
            DialogResult = Databases?.Count > 0;
        }

    }
}
