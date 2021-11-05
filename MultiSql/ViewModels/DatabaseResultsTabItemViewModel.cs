using System;
using System.Collections.ObjectModel;
using System.Data;

namespace MultiSql.ViewModels
{
    public class DatabaseResultsTabItemViewModel : TabItemViewModel
    {

        private ObservableCollection<DataTable> _resultsData;

        private Int32 _selectedDataTableCount;

        public event EventHandler<ResultTableSelectedEventArgs> ResultTableSelected;

        public ObservableCollection<DataTable> ResultsData
        {
            get => _resultsData;
            set
            {
                _resultsData = value;
                RaisePropertyChanged();
            }
        }

        public DatabaseResultsTabItemViewModel(String header, DataSet dataSet) : base(header)
        {
            ResultsData = new ObservableCollection<DataTable>();

            foreach (DataTable dataTable in dataSet.Tables)
            {
                ResultsData.Add(dataTable);
            }

        }


        public Int32 SelectedDataTableCount
        {
            get => _selectedDataTableCount;
            set
            {
                _selectedDataTableCount = value;
                RaisePropertyChanged();
                ResultTableSelected?.Invoke(this, new ResultTableSelectedEventArgs {RowCount = value});
            }
        }

    }

    public class ResultTableSelectedEventArgs : EventArgs
    {

        public Int32 RowCount { get; set; }

    }
}
