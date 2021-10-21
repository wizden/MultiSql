using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using NLog;

namespace MultiSql
{
    /// <summary>
    /// Interaction logic for ConnectServer.xaml
    /// </summary>
    public partial class ConnectServer : Window
    {

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly String WindowsAuth = "Windows Authentication";

        private readonly String SqlServerAuth = "SQL Server Authentication";

        private const String getDbListQuery =
            "SELECT name FROM sys.databases WHERE name NOT IN  ('ASPNETDB', 'ASPSTATE', 'master', 'tempdb', 'model', 'msdb', 'ReportServer', 'ReportServerTempDB') ORDER BY name;";

        private CancellationTokenSource cancellationTokenSource;

        private List<String> _databases;

        private Boolean connectionInProgress;

        public ReadOnlyCollection<String> Databases => _databases?.AsReadOnly();

        public String ServerConnectionString { get; private set; }

        public ConnectServer()
        {
            Logger.Debug("Opening Connection window.");
            InitializeComponent();
            CmbAuthenticationType.Items.Add(WindowsAuth);
            CmbAuthenticationType.Items.Add(SqlServerAuth);
            CmbAuthenticationType.SelectedIndex = 0;
            TxtServerName.Focus();
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
            connString.DataSource               = TxtServerName.Text;
            connString.IntegratedSecurity       = !TxtUserName.IsEnabled;
            connString.MultipleActiveResultSets = true;

            if (TxtUserName.IsEnabled)
            {
                connString.UserID   = TxtUserName.IsEnabled ? TxtUserName.Text : String.Empty;
                connString.Password = TxtPassword.IsEnabled ? TxtPassword.Password : String.Empty;
            }

            try
            {
                Logger.Debug($"Attempting connection. Server: {TxtServerName.Text}, Integrated Security: {connString.IntegratedSecurity.ToString()}, User: {TxtUserName.Text}.");

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
