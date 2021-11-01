using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows;
using System.Xml.Linq;
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
            InitializeComponent();
        }

    }
}
