using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace MultiSql.Common
{
    /// <summary>
    ///     Class to deal with changes in properties of items in a collection.
    /// </summary>
    /// <typeparam name="T">The type of the item that implements INotifyPropertyChanged.</typeparam>
    public class TrulyObservableCollection<T> : ObservableCollection<T> where T : INotifyPropertyChanged
    {

        #region Public Constructors

        /// <summary>
        ///     Initialises a new instance of the <see cref="TrulyObservableCollection{T}" /> class.
        /// </summary>
        public TrulyObservableCollection() => this.CollectionChanged += this.FullObservableCollectionCollectionChanged;

        /// <summary>
        ///     Initialises a new instance of the <see cref="TrulyObservableCollection{T}" /> class.
        /// </summary>
        /// <param name="items">The items in the collection</param>
        public TrulyObservableCollection(IEnumerable<T> items)
            : this()
        {
            foreach (var item in items)
            {
                this.Add(item);
            }
        }

        #endregion Public Constructors

        #region Private Methods

        /// <summary>
        ///     Setup handler for each item in collection to capture item property changes.
        /// </summary>
        /// <param name="sender">The sender object.</param>
        /// <param name="e">The NotifyCollectionChangedEventArgs object.</param>
        private void FullObservableCollectionCollectionChanged(Object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (Object item in e.NewItems)
                {
                    ((INotifyPropertyChanged) item).PropertyChanged += this.ItemPropertyChanged;
                }
            }

            if (e.OldItems != null)
            {
                foreach (Object item in e.OldItems)
                {
                    ((INotifyPropertyChanged) item).PropertyChanged -= this.ItemPropertyChanged;
                }
            }
        }

        /// <summary>
        ///     Handle the change in the property of the item.
        /// </summary>
        /// <param name="sender">The sender object.</param>
        /// <param name="e">The PropertyChangedEventArgs object.</param>
        private void ItemPropertyChanged(Object sender, PropertyChangedEventArgs e)
        {
            NotifyCollectionChangedEventArgs args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, sender, sender, IndexOf((T) sender));
            this.OnCollectionChanged(args);
        }

        #endregion Private Methods

    }
}
