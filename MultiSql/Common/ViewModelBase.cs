using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MultiSql.Common
{
    /// <summary>
    ///     Initialises a new instance of the <see cref="ViewModelBase" /> class.
    /// </summary>
    public class ViewModelBase : INotifyPropertyChanged
    {

        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged([CallerMemberName] String propertyName = null)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

    }
}
