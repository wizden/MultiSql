using System;
using MultiSql.Common;
using MultiSql.Models;

namespace MultiSql.ViewModels
{
    public class DatabaseViewModel : ViewModelBase
    {

        #region Private Fields

        private ConnectionInfo _database;

        private Boolean _isChecked;

        #endregion Private Fields

        #region Public Constructors

        public DatabaseViewModel(Int16 id, ServerViewModel server, String databaseName, Boolean integratedSecurity, String userName, DateTime lastUsedDateTime) =>
            Database = new ConnectionInfo(id, server.ServerName, integratedSecurity, userName, lastUsedDateTime);

        #endregion Public Constructors

        #region Public Properties

        public ConnectionInfo Database
        {
            get => _database;
            set
            {
                _database = value;
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
            }
        }

        #endregion Public Properties

    }
}
