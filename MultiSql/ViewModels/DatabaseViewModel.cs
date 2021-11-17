using System;
using MultiSql.Common;
using MultiSql.Models;

namespace MultiSql.ViewModels
{
    public class DatabaseViewModel : ViewModelBase
    {

        public event EventHandler QueryExecutionRequestedChanged;

        #region Private Fields

        private ConnectionInfo _database;

        private String _databaseName;

        private Boolean _isChecked;

        private Int16 _queryRetryAttempt;

        #endregion Private Fields

        #region Public Constructors

        public DatabaseViewModel(Int16 id, ServerViewModel server, String databaseName, Boolean integratedSecurity, String userName, DateTime lastUsedDateTime)
        {
            Database     = new ConnectionInfo(id, server.ServerName, integratedSecurity, userName, lastUsedDateTime);
            DatabaseName = databaseName.Replace("_", "__");
        }

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
                QueryExecutionRequestedChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public String DatabaseName
        {
            get => _databaseName;
            set
            {
                _databaseName = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        ///     Gets or sets the number of times the query on the database was attempted.
        /// </summary>
        public Int16 QueryRetryAttempt
        {
            get => _queryRetryAttempt;
            set
            {
                _queryRetryAttempt = value;
                RaisePropertyChanged();
            }
        }

        #endregion Public Properties

    }
}
