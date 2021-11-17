using System;
using MultiSql.Common;

namespace MultiSql.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {

        private          ViewModelBase          _selectedViewModel;
        private readonly MultiSqlViewModel      _multiSqlViewModel;
        private readonly ConnectServerViewModel _connectServerViewModel;
        private static   Int16                  id = 0;

        public ViewModelBase SelectedViewModel
        {
            get => _selectedViewModel;

            set
            {
                _selectedViewModel = value;
                RaisePropertyChanged();
            }
        }

        public MainWindowViewModel()
        {
            _connectServerViewModel                   =  new ConnectServerViewModel();
            _connectServerViewModel.ConnectionChanged += ConnectServerViewModel_ConnectionChanged;

            _multiSqlViewModel                                        =  new MultiSqlViewModel();
            _multiSqlViewModel.DatabaseListViewModel.ChangeConnection += DatabaseListViewModel_ChangeConnection;

            SelectedViewModel = _connectServerViewModel;

        }

        private void ConnectServerViewModel_ConnectionChanged(Object sender, EventArgs e)
        {
            var connectServer = sender as ConnectServerViewModel;

            if (!String.IsNullOrWhiteSpace(connectServer.ServerConnectionString))
            {
                var issues = _multiSqlViewModel.DatabaseListViewModel.AddServer(connectServer);

                ////_multiSqlViewModel.DatabaseListViewModel.AllDatabases.Clear();
                ////_multiSqlViewModel.DatabaseListViewModel.ConnectionStringBuilder = new SqlConnectionStringBuilder(connectServer.ServerConnectionString);
                ////var databaseList = new ObservableCollection<DbInfo>();

                ////foreach (var database in connectServer.Databases)
                ////{
                ////    var dbInfo = new DbInfo(connectServer.ServerName, database);
                ////    databaseList.Add(dbInfo);
                ////}

                ////_multiSqlViewModel.DatabaseListViewModel.AllDatabases = databaseList;
            }

            SelectedViewModel = _multiSqlViewModel;
        }

        private void DbInfo_QueryExecutionRequestedChanged(Object sender, EventArgs e)
        {
            var t = "test";
        }

        private void DatabaseListViewModel_ChangeConnection(Object sender, EventArgs e)
        {
            SelectedViewModel = _connectServerViewModel;
        }

    }
}
