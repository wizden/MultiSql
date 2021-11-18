using System;
using MultiSql.Common;
using MultiSql.Models;

namespace MultiSql.ViewModels
{
    public abstract class TabItemViewModel : ViewModelBase, ITabItem
    {

        public TabItemViewModel(String header) => Header = header;

        public String Header { get; set; }

        public String Description { get; set; }

    }
}
