using System;
using System.Windows;
using System.Windows.Controls;
using MultiSql.ViewModels;

namespace MultiSql.Views
{
    /// <summary>
    ///     Interaction logic for MultiSqlView.xaml
    /// </summary>
    public partial class MultiSqlView : UserControl
    {

        #region Public Constructors

        /// <summary>
        ///     Initialises a new instance of the <see cref="MultiSqlWindow" /> class.
        /// </summary>
        public MultiSqlView()
        {
            InitializeComponent();
        }

        #endregion Public Constructors

        private void TxtBlkResults_OnTextChanged(Object sender, TextChangedEventArgs e)
        {
            TxtBlkResults.ScrollToEnd();
        }

        private void TxtQuery_OnSelectionChanged(Object sender, RoutedEventArgs e)
        {
            if (DataContext is MultiSqlViewModel)
            {
                var dc = DataContext as MultiSqlViewModel;
                dc.QuerySelectedText = String.IsNullOrWhiteSpace(TxtQuery.SelectedText) ? String.Empty : TxtQuery.SelectedText;
            }
        }

    }
}
