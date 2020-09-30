using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace Atlas.Common.Sql
{
    public class TempTableFilterDetails
    {
        /// <summary>
        /// A JOIN query that can be used on a query to filter by the specified ids.
        /// Example usage:
        ///
        /// $"SELECT * FROM MyTable {FilteredJoinQueryString}}" 
        /// </summary>
        public string FilteredJoinQueryString { get; set; }

        /// <summary>
        /// A factory that will create and populate the temp table, given a SQL Connection
        /// </summary>
        public Func<SqlConnection, Task> BuildTempTableFactory { get; set; }
    }

    public static class SqlTempTableFiltering
    {
        /// <summary>
        /// For SQL queries of the form WHERE x IN (y1, y2...), when the number of values is very large, the query becomes infeasibly slow to run.
        /// Quicker is to create a temp table of ids and join to it.
        /// 
        /// This method creates such a temp table, using SqbBulkCopy for speedy inserts.
        /// 
        /// Usage:
        ///
        /// (a) In your query, RIGHT JOIN the column being filtered to this table.
        /// (b) Once an SqlConnection has been created, call the returned factory to create and populate the temp table before running your query.
        /// </summary>
        /// <param name="filteredTableAlias">Used in the join string to join your table by alias to the temp table.</param>
        /// <param name="filteredColumnName">Column name to filter</param>
        /// <param name="ids">The ids to filter by</param>
        /// <param name="tempTableName">
        ///     By default, will create a temp table named "temp".
        ///     If specified, a custom name can be used for the temp join table - e.g. if multiple temp tables are needed within the same connection.
        /// </param>
        public static TempTableFilterDetails PrepareTempTableFiltering(
            string filteredTableAlias,
            string filteredColumnName,
            IEnumerable<int> ids,
            string tempTableName = "temp")
        {
            if (!tempTableName.StartsWith("#"))
            {
                tempTableName = $"#{tempTableName}";
            }

            if (tempTableName.StartsWith("##"))
            {
                throw new Exception("Must use non-global temp tables. Please do not use a table name beginning with ##.");
            }

            const string idColumnName = "id";

            var joinString = $"RIGHT JOIN {tempTableName} ON {tempTableName}.{idColumnName} = {filteredTableAlias}.{filteredColumnName}";

            async Task TableFactory(SqlConnection connection)
            {
                if (connection.State != ConnectionState.Open)
                {
                    connection.Open();
                }

                var cmd = new SqlCommand($"CREATE TABLE {tempTableName} ({idColumnName} int)", connection);
                cmd.ExecuteNonQuery();
                var dataTable = new DataTable(tempTableName);
                var id = new DataColumn {DataType = Type.GetType("System.Int32"), ColumnName = idColumnName};
                dataTable.Columns.Add(id);
                foreach (var item in ids)
                {
                    var row = dataTable.NewRow();
                    row[0] = item;
                    dataTable.Rows.Add(row);
                }

                using (var bulkCopy = new SqlBulkCopy(connection))
                {
                    bulkCopy.DestinationTableName = tempTableName;
                    await bulkCopy.WriteToServerAsync(dataTable);
                }
            }

            return new TempTableFilterDetails
            {
                BuildTempTableFactory = TableFactory,
                FilteredJoinQueryString = joinString
            };
        }
    }
}