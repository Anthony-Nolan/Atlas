using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nova.SearchAlgorithm.Data.Context;
using Nova.SearchAlgorithm.Data.Persistent;
using System;

namespace Nova.SearchAlgorithm.Test.Integration.TestHelpers
{
    public class DatabaseManager
    {
        /// <summary>
        /// Creates if necessary, and runs migrations on both a transient and persistent database
        /// </summary>
        public static void SetupDatabase()
        {
            var transientContext = DependencyInjection.DependencyInjection.Provider.GetService<SearchAlgorithmContext>();
            var persistentContext = DependencyInjection.DependencyInjection.Provider.GetService<SearchAlgorithmPersistentContext>();

            var connectionStringB = DependencyInjection.DependencyInjection.Provider.GetService<IConfiguration>().GetSection("ConnectionStrings")["SqlB"];
            var transientContextB = new Data.Context.ContextFactory().Create(connectionStringB);
            
            if (transientContext == null || persistentContext == null)
            {
                throw new Exception("Database context could resolved - DI has not been correctly configured.");
            }

            transientContext.Database.Migrate();
            transientContextB?.Database.Migrate();
            persistentContext.Database.Migrate();
        }
        
        /// <summary>
        /// Clears the test database of data. Can be accessed by fixtures to run after each fixture, but not after each test.
        /// </summary>
        public static void ClearDatabases()
        {
            var transientContextA = DependencyInjection.DependencyInjection.Provider.GetService<SearchAlgorithmContext>();
            ClearDatabase(transientContextA);
            
            var connectionStringB = DependencyInjection.DependencyInjection.Provider.GetService<IConfiguration>().GetSection("ConnectionStrings")["SqlB"];
            var transientContextB = new Data.Context.ContextFactory().Create(connectionStringB);
            ClearDatabase(transientContextB);
        }

        private static void ClearDatabase(DbContext context)
        {
            if (context != null)
            {
                context.Database.ExecuteSqlCommand("TRUNCATE TABLE [Donors]");
                context.Database.ExecuteSqlCommand("TRUNCATE TABLE [MatchingHlaAtA]");
                context.Database.ExecuteSqlCommand("TRUNCATE TABLE [MatchingHlaAtB]");
                context.Database.ExecuteSqlCommand("TRUNCATE TABLE [MatchingHlaAtC]");
                context.Database.ExecuteSqlCommand("TRUNCATE TABLE [MatchingHlaAtDrb1]");
                context.Database.ExecuteSqlCommand("TRUNCATE TABLE [MatchingHlaAtDqb1]");
                context.Database.ExecuteSqlCommand("DELETE FROM PGroupNames");
            }
        }
    }
}