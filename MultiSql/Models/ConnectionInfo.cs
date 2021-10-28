using System;

namespace MultiSql.Models
{
    /// <summary>
    ///     Class to retain connection information.
    /// </summary>
    public class ConnectionInfo
    {

        /// <summary>
        ///     Gets or sets the connection server name.
        /// </summary>
        public String ServerName { get; set; }

        /// <summary>
        ///     Gets or sets a boolean indicating whether integrated security is used.
        /// </summary>
        public Boolean IntegratedSecurity { get; set; }

        /// <summary>
        ///     Gets or sets the connection user name.
        /// </summary>
        public String UserName { get; set; }

        /// <summary>
        ///     Gets or sets the date and time that the connection was last used.
        /// </summary>
        public DateTime LastUsedDateTime { get; set; }

        public ConnectionInfo(String serverName, Boolean integratedSecurity, String userName, DateTime lastUsedDateTime)
        {
            ServerName         = serverName;
            IntegratedSecurity = integratedSecurity;
            UserName           = userName;
            LastUsedDateTime   = lastUsedDateTime;
        }

    }
}
