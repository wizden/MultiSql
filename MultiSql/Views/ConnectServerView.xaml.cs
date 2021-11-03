using System;
using System.Windows;
using System.Windows.Controls;
using MultiSql.ViewModels;

namespace MultiSql.Views
{
    /// <summary>
    /// Interaction logic for ConnectServerView.xaml
    /// </summary>
    public partial class ConnectServerView : UserControl
    {
        public ConnectServerView()
        {
            InitializeComponent();
            CmbAuthenticationType.SelectedIndex = 0;
        }

        private void TxtPassword_OnPasswordChanged(Object sender, RoutedEventArgs e)
        {
            if (DataContext != null && DataContext is ConnectServerViewModel)
            {
                ((ConnectServerViewModel) DataContext).Password = TxtPassword.SecurePassword;
            }
        }

    }
}
