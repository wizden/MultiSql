using System;
using System.Collections.Generic;

namespace MultiSql.Common
{
    /// <summary>
    ///     Class to store the result of each individual query as string.
    /// </summary>
    public class Result
    {

        /// <summary>
        ///     Private store for each individual row in the result.
        /// </summary>
        private readonly List<String> rowLines = new();

        /// <summary>
        ///     Gets or sets the row header of the result of the individual query.
        /// </summary>
        public String Header { get; set; }

        /// <summary>
        ///     Gets the row lines concatenated based on the result of the individual query.
        /// </summary>
        public String Rows => String.Join(Environment.NewLine, rowLines);

        /// <summary>
        ///     Add a new row to the display result.
        /// </summary>
        /// <param name="row">The row in string format (could be comma/space/tab separated.</param>
        public void AddRow(String row)
        {
            rowLines.Add(row);
        }

    }
}
