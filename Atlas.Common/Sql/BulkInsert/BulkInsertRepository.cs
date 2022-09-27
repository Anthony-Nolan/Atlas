using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace Atlas.Common.Sql.BulkInsert
{
    public interface IBulkInsertRepository<in TEntity> where TEntity : IBulkInsertModel
    {
        Task BulkInsert(IReadOnlyCollection<TEntity> entities);
    }

    public abstract class BulkInsertRepository<TEntity> : IBulkInsertRepository<TEntity> where TEntity : IBulkInsertModel
    {
        private static readonly IReadOnlyCollection<PropertyInfo> Properties = typeof(TEntity).GetProperties();

        protected readonly string ConnectionString;
        private readonly string bulkInsertTableName;

        protected BulkInsertRepository(string connectionString, string bulkInsertTableName)
        {
            ConnectionString = connectionString;
            this.bulkInsertTableName = bulkInsertTableName;
        }

        public async Task BulkInsert(IReadOnlyCollection<TEntity> entities)
        {
            if (!entities.Any())
            {
                return;
            }

            var columnNames = GetColumnNames();
            var dataTable = BuildDataTable(entities, columnNames);

            using (var sqlBulk = BuildSqlBulkCopy(columnNames))
            {
                await sqlBulk.WriteToServerAsync(dataTable);
            }
        }

        private static IReadOnlyCollection<string> GetColumnNames()
        {
            var columns = Properties.Select(p => p.Name).ToList();
            columns.Remove(nameof(IBulkInsertModel.Id));
            return columns;
        }

        private DataTable BuildDataTable(IEnumerable<TEntity> entities, IReadOnlyCollection<string> columnNames)
        {
            var dataTable = new DataTable();

            foreach (var columnName in columnNames)
            {
                dataTable.Columns.Add(columnName);
            }

            foreach (var entity in entities)
            {
                dataTable.Rows.Add(GetValues(columnNames, entity));
            }

            return dataTable;
        }

        private static object?[] GetValues(IReadOnlyCollection<string> columnNames, TEntity entity)
        {
            var values = new object?[columnNames.Count];

            for (var i = 0; i < columnNames.Count; i++)
            {
                var property = Properties.Single(p => p.Name == columnNames.ElementAt(i));
                values[i] = property.GetValue(entity);
            }

            return values;
        }

        private SqlBulkCopy BuildSqlBulkCopy(IEnumerable<string> columnNames)
        {
            var sqlBulk = new SqlBulkCopy(ConnectionString)
            {
                BulkCopyTimeout = 3600,
                BatchSize = 10000,
                DestinationTableName = bulkInsertTableName
            };

            foreach (var columnName in columnNames)
            {
                // Relies on setting up the data table with column names matching the database columns.
                sqlBulk.ColumnMappings.Add(columnName, columnName);
            }

            return sqlBulk;
        }
    }
}
