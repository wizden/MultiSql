using System;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Windows.Data;
using MultiSql.Common;
using NLog;

namespace MultiSql.ViewModels
{
    public class ServerViewModel : ViewModelBase
    {

        #region Private Fields

        /// <summary>
        ///     Private store for the Logger object.
        /// </summary>
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private ObservableCollection<DatabaseViewModel> _databases;
        private Boolean                                 _isChecked;
        private RelayCommand                            _cmdConnectToSsms;
        private RelayCommand                            _cmdDisconnect;

        #endregion Private Fields

        public event EventHandler Disconnect;

        #region Public Constructors

        static ServerViewModel() { }

        public ServerViewModel(SqlConnectionStringBuilder connectionStringBuilder, SqlCredential connectionCredential)
        {
            ConnectionStringBuilder = connectionStringBuilder;
            Databases               = new ObservableCollection<DatabaseViewModel>();
            ServerName              = ConnectionStringBuilder.DataSource;
            UserName                = ConnectionStringBuilder.UserID;
            IntegratedSecurity      = ConnectionStringBuilder.IntegratedSecurity;
            ConnectionCredential    = connectionCredential;


        }

        #endregion Public Constructors

        #region Public Properties

        public readonly SqlConnectionStringBuilder ConnectionStringBuilder;

        public SqlCredential ConnectionCredential { get; }

        public ObservableCollection<DatabaseViewModel> Databases
        {
            get => _databases;
            set
            {
                _databases = value;
                RaisePropertyChanged();
            }
        }

        public ListCollectionView DatabasesView => (ListCollectionView)CollectionViewSource.GetDefaultView(Databases);

        public String Description => $"Name: {ConnectionStringBuilder.DataSource}, Integrated Security: {ConnectionStringBuilder.IntegratedSecurity}" +
                                     (!String.IsNullOrWhiteSpace(ConnectionCredential?.UserId) ? $", User ID: {ConnectionCredential.UserId}" : String.Empty);

        public Boolean IntegratedSecurity { get; }

        public Boolean IsChecked
        {
            get => _isChecked;
            set
            {
                _isChecked = value;
                RaisePropertyChanged();

                foreach (var database in Databases)
                {
                    database.IsChecked = value;
                }
            }
        }

        public String ServerName { get; }

        public String UserName { get; }

        /// <summary>
        ///     Command to connect to Management Studio.
        /// </summary>
        public RelayCommand CmdConnectToSsms
        {
            get { return _cmdConnectToSsms ??= new RelayCommand(execute => ConnectToSsms(), canExecute => true); }
        }

        /// <summary>
        ///     Command to connect to Management Studio.
        /// </summary>
        public RelayCommand CmdDisconnect
        {
            get { return _cmdDisconnect ??= new RelayCommand(execute => DisconnectServer(), canExecute => true); }
        }

        private void DisconnectServer()
        {
            Disconnect?.Invoke(this, EventArgs.Empty);
        }

        private void ConnectToSsms()
        {
            MultiSqlSettings.ConnectToSsms(ServerName, String.Empty);

        }

        #endregion Public Properties

    }
}
