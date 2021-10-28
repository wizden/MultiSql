using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;

namespace MultiSql.Common
{
    /// <summary>
    ///     Class to handle common scenarios.
    /// </summary>
    class MultiSqlSettings
    {

        #region Private Fields

        /// <summary>
        ///     Private store for the application name.
        /// </summary>
        private static String applicationName = String.Empty;

        /// <summary>
        ///     Private store for the application base directory.
        /// </summary>
        private static String appBaseDirectory = String.Empty;

        /// <summary>
        ///     Private store for the path to the file containing the connection list.
        /// </summary>
        private static readonly String connectionsListFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Connections.xml");

        #endregion Private Fields

        #region Public Enums

        /// <summary>
        ///     Enumerator for different result types.
        /// </summary>
        public enum ResultDisplayType
        {

            /// <summary>
            ///     Save result to different tabs.
            /// </summary>
            DifferentTabs = 0,

            /// <summary>
            ///     Save result to text.
            /// </summary>
            Text = 1,

            /// <summary>
            ///     Save result to file named per database.
            /// </summary>
            DatabaseFileName = 2,

            /// <summary>
            ///     Save result to a single file with results from all databases.
            /// </summary>
            CombinedFile = 3,

            /// <summary>
            ///     Save result to text without headers.
            /// </summary>
            TextFirstHeaderOnly = 4,

            /// <summary>
            ///     Save result to text with SSMS like text formatting.
            /// </summary>
            TextSqlFormatted = 5

        }

        #endregion Public Enums

        #region Public Properties

        /// <summary>
        ///     Gets the base path of the application.
        /// </summary>
        public static String AppBaseDirectory
        {
            get
            {
                if (appBaseDirectory == String.Empty)
                {
                    appBaseDirectory = Directory.GetParent(Environment.CurrentDirectory).Parent.FullName + "\\";
                }

                return appBaseDirectory;
            }
        }

        /// <summary>
        ///     Gets the name of the application.
        /// </summary>
        /// <returns></returns>
        public static String ApplicationName
        {
            get
            {
                if (applicationName == String.Empty)
                {
                    applicationName = Process.GetCurrentProcess().ProcessName;
                }

                return applicationName;
            }
        }

        /// <summary>
        ///     Gets the file path for the list of connections.
        /// </summary>
        public static String ConnectionsListFile => connectionsListFile;

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        ///     Get a SQL connection string.
        /// </summary>
        /// <param name="serverNameIp">The name or IP of the server.</param>
        /// <param name="database">The database name.</param>
        /// <param name="userId">The user id.</param>
        /// <param name="password">The password for the user.</param>
        /// <param name="integratedSecurity">If set to true, username and password are ignored.</param>
        /// <param name="connectionTimeout">The connection timeout period.</param>
        /// <returns>The SQL connection string.</returns>
        public static String GetSqlConnectionString(String serverNameIp, String database, String userId, Boolean integratedSecurity, Int32 connectionTimeout = 30)
        {
            var connStringBuilder = new SqlConnectionStringBuilder
                                    {
                                        ApplicationName          = "Multi Sql",
                                        ConnectTimeout           = connectionTimeout,
                                        DataSource               = serverNameIp,
                                        Enlist                   = true,
                                        InitialCatalog           = database,
                                        IntegratedSecurity       = integratedSecurity,
                                        MultipleActiveResultSets = true,
                                        PersistSecurityInfo      = false,
                                        Pooling                  = false
                                    };

            if (!connStringBuilder.IntegratedSecurity)
            {
                connStringBuilder.UserID = userId;
            }

            return connStringBuilder.ConnectionString;
        }

        #endregion Public Methods

    }
}
