using Microsoft.EntityFrameworkCore;
using System.Data;
using Atlas.SearchTracking.Data.Context;
using Microsoft.Data.Sqlite;

namespace Atlas.SearchTracking.Data.Test.TestHelpers
{
    public static class SqliteMemoryContext
    {
        /// <summary>
        /// Connection must be closed in TearDown
        /// </summary>
        public static (IDbConnection, SearchTrackingContext) Create()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            var options = new DbContextOptionsBuilder<SearchTrackingContext>()
                .UseSqlite(connection).Options;

            return (connection, new SearchTrackingContext(options));
        }
    }
}
