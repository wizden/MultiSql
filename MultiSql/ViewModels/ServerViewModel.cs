using System;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using MultiSql.Common;

namespace MultiSql.ViewModels
{
    public class ServerViewModel : ViewModelBase
    {

        #region Private Fields

        private ObservableCollection<DatabaseViewModel> _databases;
        private Boolean                                 _isChecked;

        #endregion Private Fields

        #region Public Constructors

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

        #endregion Public Properties

    }
}
