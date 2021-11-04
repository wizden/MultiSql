using System;
using System.ComponentModel;

namespace MultiSql
{
    public class DbInfo : INotifyPropertyChanged
    {

        public event EventHandler QueryExecutionRequestedChanged;

        #region Private Fields

        /// <summary>
        ///     Private store to determine whether the query is to be executed for the database info object.
        /// </summary>
        private Boolean queryExecutionRequested;

        #endregion Private Fields

        #region Public Constructors

        /// <summary>
        ///     Initialises a new instance of the <see cref="DbInfo" /> class.
        /// </summary>
        public DbInfo() { }

        /// <summary>
        ///     Initialises a new instance of the <see cref="DbInfo" /> class.
        /// </summary>
        /// <param name="server">The database server name.</param>
        /// <param name="database">The database name.</param>
        public DbInfo(String server, String database)
            : this()
        {
            Server   = server;
            Database = database;
        }

        #endregion Public Constructors

        #region Public Events

        /// <summary>
        ///     Event handler for the Property changed event.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion Public Events

        #region Public Properties

        /// <summary>
        ///     Gets the database name.
        /// </summary>
        public String Database { get; }

        /// <summary>
        ///     Gets or sets a value indicating whether the query is to be executed against the database.
        /// </summary>
        public Boolean QueryExecutionRequested
        {
            get => queryExecutionRequested;

            set
            {
                queryExecutionRequested = value;
                NotifyPropertyChanged("QueryExecutionRequested");
                QueryExecutionRequestedChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        ///     Gets or sets the number of times the query on the database was attempted.
        /// </summary>
        public Int32 QueryRetryAttempt { get; set; }

        /// <summary>
        ///     Gets the database server name.
        /// </summary>
        public String Server { get; }

        #endregion Public Properties

        #region Protected Methods

        /// <summary>
        ///     The Notify Property Changed event handler method.
        /// </summary>
        /// <param name="strPropertyName">The name of the property</param>
        protected void NotifyPropertyChanged(String strPropertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(strPropertyName));
            }
        }

        #endregion Protected Methods

    }
}
