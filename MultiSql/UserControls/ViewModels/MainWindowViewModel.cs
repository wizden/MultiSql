using System;
using MultiSql.Common;

namespace MultiSql.UserControls.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {

        private          ViewModelBase          _selectedViewModel;
        private readonly MultiSqlViewModel      _multiSqlViewModel;
        private readonly ConnectServerViewModel _connectServerViewModel;

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

            foreach (var database in connectServer.Databases)
            {
                _multiSqlViewModel.DatabaseListViewModel.AllDatabases.Add(new DbInfo(connectServer.ServerName, database));
            }

            SelectedViewModel                                     = _multiSqlViewModel;
        }

        private void DatabaseListViewModel_ChangeConnection(Object sender, EventArgs e)
        {
            SelectedViewModel = _connectServerViewModel;
        }

    }
}
