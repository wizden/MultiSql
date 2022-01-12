using System;
using System.Collections.ObjectModel;
using MultiSql.Common;

namespace MultiSql.ViewModels
{
    public class ConnectionsViewModel : ViewModelBase
    {

        #region Private Fields

        /// <summary>
        ///     Private store for the command to add a new connection.
        /// </summary>
        private RelayCommand _cmdAddConnection;

        /// <summary>
        ///     Private store for the list of connected servers.
        /// </summary>
        private ObservableCollection<ServerViewModel> _servers;

        #endregion Private Fields

        #region Public Events

        public event EventHandler ChangeConnection;

        #endregion Public Events

        #region Public Properties

        public RelayCommand CmdAddConnection
        {
            get { return _cmdAddConnection ??= new RelayCommand(execute => AddSQLConnection(), canExecute => true); }
        }

        /// <summary>
        ///     Gets or sets the list of connected servers.
        /// </summary>
        public ObservableCollection<ServerViewModel> Servers
        {
            get => _servers;
            set
            {
                _servers = value;
                RaisePropertyChanged();
            }
        }

        #endregion Public Properties

        #region Private Methods

        /// <summary>
        ///     Change the connection for the control.
        /// </summary>
        private void AddSQLConnection()
        {
            ChangeConnection?.Invoke(this, EventArgs.Empty);
        }

        #endregion Private Methods

    }
}
