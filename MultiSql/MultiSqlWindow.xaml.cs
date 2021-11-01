using System;
using System.Reflection;
using System.Security.Principal;
using System.Windows;
using System.Windows.Input;

namespace MultiSql
{
    /// <summary>
    ///     Interaction logic for MultiSqlWindow.xaml
    /// </summary>
    public partial class MultiSqlWindow : Window
    {

        #region Public Constructors

        /// <summary>
        ///     Initialises a new instance of the <see cref="MultiSqlWindow" /> class.
        /// </summary>
        public MultiSqlWindow()
        {
            InitializeComponent();
            SetRunningUserInfo();
        }

        #endregion Public Constructors

        #region Private Methods

        /// <summary>
        ///     Sets the title bar to display details of user running the application.
        /// </summary>
        private void SetRunningUserInfo()
        {
            var assemblyTitleAttribute = (AssemblyTitleAttribute) Attribute.GetCustomAttribute(Assembly.GetExecutingAssembly(), typeof(AssemblyTitleAttribute), false);
            var programName            = assemblyTitleAttribute != null ? assemblyTitleAttribute.Title : "Unknown Assembly Name";
            var userName               = String.Format(@"{0}\{1}", Environment.UserDomainName, Environment.UserName);
            var principal              = new WindowsPrincipal(WindowsIdentity.GetCurrent());

            if (principal.IsInRole(WindowsBuiltInRole.Administrator))
            {
                userName += " - Administrator";
            }

            Title = String.Format("{0} ({1})", programName, userName);
        }

        /// <summary>
        ///     Capture key press on form.
        /// </summary>
        /// <param name="sender">The sender object.</param>
        /// <param name="e">The KeyEventArgs object.</param>
        private void Window_PreviewKeyDown(Object sender, KeyEventArgs e)
        {
            ////if (e.Key == Key.F5)
            ////{
            ////    if (BtnRunQuery.Command != null && BtnRunQuery.Command.CanExecute(null))
            ////    {
            ////        BtnRunQuery.Command.Execute(null);
            ////    }
            ////}
            ////else if (e.Key == Key.F6)
            ////{
            ////    if (BtnRunQuery.Command != null && BtnRunQuery.Command.CanExecute(null))
            ////    {
            ////        TxtQuery.Focus();
            ////        var selectionStart = TxtQuery.SelectionStart;
            ////        var selectionEnd   = TxtQuery.SelectionStart;

            ////        if (selectionStart == TxtQuery.Text.Length)
            ////        {
            ////            selectionStart = 0;
            ////        }

            ////        if (selectionEnd == TxtQuery.Text.Length)
            ////        {
            ////            selectionEnd--;
            ////        }

            ////        var canProcess      = false;
            ////        var processAttempts = 0;

            ////        while (!canProcess && processAttempts < 3)
            ////        {
            ////            while (selectionStart >= 0 && (TxtQuery.Text[selectionStart] != '\r' || selectionStart >= 2 && TxtQuery.Text[selectionStart - 2] != '\r'))
            ////            {
            ////                if (selectionStart == 0)
            ////                {
            ////                    break;
            ////                }

            ////                selectionStart--;
            ////            }

            ////            selectionStart -= 2;

            ////            if (selectionStart < 0)
            ////            {
            ////                selectionStart = 0;
            ////            }

            ////            while (selectionEnd + 2 < TxtQuery.Text.Length && (TxtQuery.Text[selectionEnd] != '\r' || TxtQuery.Text[selectionEnd + 2] != '\r'))
            ////            {
            ////                if (selectionEnd == TxtQuery.Text.Length - 1)
            ////                {
            ////                    selectionEnd++;
            ////                    break;
            ////                }

            ////                selectionEnd++;
            ////            }

            ////            if (Math.Abs(selectionEnd - TxtQuery.Text.Length) <= 2)
            ////            {
            ////                selectionEnd = TxtQuery.Text.Length;
            ////            }

            ////            selectionStart += selectionStart > 0 ? 4 : 0;
            ////            canProcess     =  selectionStart >= 0 && selectionEnd <= TxtQuery.Text.Length && selectionStart < selectionEnd;
            ////            processAttempts++;

            ////            if (!canProcess && selectionStart > 10)
            ////            {
            ////                selectionStart -= 9;
            ////            }
            ////        }

            ////        if (canProcess)
            ////        {
            ////            TxtQuery.Select(selectionStart, selectionEnd - selectionStart);

            ////            if (TxtQuery.SelectedText.Replace(Environment.NewLine, String.Empty).Length > 0)
            ////            {
            ////                BtnRunQuery.Command.Execute(null);
            ////            }
            ////        }
            ////    }
            ////}
        }

        #endregion Private Methods

    }
}
