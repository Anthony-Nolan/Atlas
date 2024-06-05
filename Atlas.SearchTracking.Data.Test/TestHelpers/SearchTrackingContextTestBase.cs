using System.Data;
using Atlas.SearchTracking.Data.Context;
using Atlas.SearchTracking.Data.Test.Builders;

namespace Atlas.SearchTracking.Data.Test.TestHelpers
{
    public abstract class SearchTrackingContextTestBase
    {
        private IDbConnection connection;

        protected SearchTrackingContext SearchTrackingContext;

        protected virtual async Task SetUpBase()
        {
            (connection, SearchTrackingContext) = SqliteMemoryContext.Create();
            await SearchTrackingContext.Database.EnsureCreatedAsync();
        }

        protected async Task TearDownBase()
        {
            await SearchTrackingContext.DisposeAsync();
            connection.Close();
            connection.Dispose();
        }

        protected async Task InitiateData()
        {
            await SearchTrackingContext.AddAsync(SearchRequestEntityBuilder.Default.Build());
            await SearchTrackingContext.SaveChangesAsync();
        }
    }
}
