using System;
using System.Collections.ObjectModel;
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

        public ServerViewModel(String serverName, String userName, Boolean integratedSecurity, DateTime lastUsedDateTime)
        {
            Databases          = new ObservableCollection<DatabaseViewModel>();
            ServerName         = serverName;
            UserName           = userName;
            IntegratedSecurity = integratedSecurity;
        }

        #endregion Public Constructors

        #region Public Properties

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
