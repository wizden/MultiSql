using System;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using NLog;

namespace MultiSql.Common
{
    /// <summary>
    ///     Class to handle common scenarios.
    /// </summary>
    public class MultiSqlSettings
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

        /// <summary>
        ///     Private store for the Logger object.
        /// </summary>
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        ///     Private store for the location of the file that stores where the SSMS executable resides.
        /// </summary>
        private static readonly String ssmsFilePathInfo = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SsmsPath.txt");

        /// <summary>
        ///     Private store of the location of the SSMS executable.
        /// </summary>
        private static readonly String ssmsExecutablePath;

        #endregion Private Fields

        static MultiSqlSettings()
        {
            try
            {
                /*
                 * Every version of SSMS seems to be in a different path, so no one size fits all solution:
                 * https://aprentis.net/sql-server-management-studio-ssms-exe-executable-file-location/
                 */

                ssmsExecutablePath = File.ReadAllText(ssmsFilePathInfo);
                Logger.Debug($"SSMS executable path set to '{ssmsExecutablePath}'.");
            }
            catch (Exception)
            {
                // Set default path if any file exception is thrown.
                ssmsExecutablePath = @"C:\Program Files (x86)\Microsoft SQL Server Management Studio 18\Common7\IDE\ssms.exe";
                Logger.Debug($"Setting default path of the SSMS executable to '{ssmsExecutablePath}' in '{ssmsFilePathInfo}'. " +
                             $"If the path to the executable is not correct, please set the correct value in '{ssmsFilePathInfo}'.");

                try
                {
                    File.WriteAllText(ssmsFilePathInfo, ssmsExecutablePath);
                }
                catch (Exception fileWriteException)
                {
                    Logger.Error(fileWriteException, $"Unable to write SSMS executable path location '{ssmsExecutablePath}' to '{ssmsFilePathInfo}'.");
                }
            }
        }

        #region Public Enums

        /// <summary>
        ///     Enumerator for different result types.
        /// </summary>
        public enum ResultDisplayType
        {

            /// <summary>
            ///     Save result to different tabs.
            /// </summary>
            [Description("Different Tabs")] DifferentTabs = 0,

            /// <summary>
            ///     Save result to text.
            /// </summary>
            [Description("Text")] Text = 1,

            /// <summary>
            ///     Save result to file named per database.
            /// </summary>
            [Description("File Per Database")] DatabaseFileName = 2,

            /// <summary>
            ///     Save result to a single file with results from all databases.
            /// </summary>
            [Description("Combined File")] CombinedFile = 3,

            /// <summary>
            ///     Save result to text without headers.
            /// </summary>
            [Description("Single Header Results")] TextFirstHeaderOnly = 4,

            /// <summary>
            ///     Save result to text with SSMS like text formatting.
            /// </summary>
            [Description("SQL Formatted")] TextSqlFormatted = 5

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

        /// <summary>
        ///     Connect to Sql Server Management Studio.
        /// </summary>
        /// <param name="serverName">The server name.</param>
        /// <param name="databaseName">The parameter is not used.</param>
        public static void ConnectToSsms(String serverName, String databaseName)
        {
            try
            {
                Process.Start(new ProcessStartInfo(ssmsExecutablePath, $"-S {serverName}"));
            }
            catch (Exception e)
            {
                e.Data.Add("Ssms Executable Path", ssmsExecutablePath);
                Logger.Error(e, $"Unable to open ssms.exe. Please locate ssms.exe on the machine and save the full path to '{ssmsFilePathInfo}'.");
            }
        }

        #endregion Public Methods

    }
}
