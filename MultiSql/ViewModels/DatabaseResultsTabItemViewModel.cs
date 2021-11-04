using System;
using System.Collections.ObjectModel;
using System.Data;

namespace MultiSql.ViewModels
{
    public class DatabaseResultsTabItemViewModel : TabItemViewModel
    {

        private ObservableCollection<DataTable> _resultsData;

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

    }
}
