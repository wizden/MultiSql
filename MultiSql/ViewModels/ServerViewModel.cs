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

        public ServerViewModel(SqlConnectionStringBuilder connectionStringBuilder)
        {
            ConnectionStringBuilder = connectionStringBuilder;
            Databases               = new ObservableCollection<DatabaseViewModel>();
            ServerName              = ConnectionStringBuilder.DataSource;
            UserName                = ConnectionStringBuilder.UserID;
            IntegratedSecurity      = ConnectionStringBuilder.IntegratedSecurity;
        }

        #endregion Public Constructors

        #region Public Properties

        public readonly SqlConnectionStringBuilder ConnectionStringBuilder;

        public ObservableCollection<DatabaseViewModel> Databases
        {
            get => _databases;
            set
            {
                _databases = value;
                RaisePropertyChanged();
            }
        }

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

        public Boolean IntegratedSecurity { get; }

        #endregion Public Properties

    }
}
