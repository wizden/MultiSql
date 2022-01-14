using System;
using System.Reflection;
using System.Security.Principal;
using System.Windows;

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
            var assemblyTitleAttribute = (AssemblyTitleAttribute)Attribute.GetCustomAttribute(Assembly.GetExecutingAssembly(), typeof(AssemblyTitleAttribute), false);
            var programName            = assemblyTitleAttribute != null ? assemblyTitleAttribute.Title : "Unknown Assembly Name";
            var userName               = String.Format(@"{0}\{1}", Environment.UserDomainName, Environment.UserName);
            var principal              = new WindowsPrincipal(WindowsIdentity.GetCurrent());

            if (principal.IsInRole(WindowsBuiltInRole.Administrator))
            {
                userName += " - Administrator";
            }

            Title = String.Format("{0} ({1})", programName, userName);
        }

        #endregion Private Methods

    }
}
