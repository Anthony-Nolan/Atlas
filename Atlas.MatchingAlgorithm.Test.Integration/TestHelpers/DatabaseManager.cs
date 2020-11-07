using System;
using System.Threading.Tasks;
using Atlas.MatchingAlgorithm.Data.Context;
using Atlas.MatchingAlgorithm.Data.Persistent.Context;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.ConnectionStringProviders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ContextFactory = Atlas.MatchingAlgorithm.Data.Context.ContextFactory;

namespace Atlas.MatchingAlgorithm.Test.Integration.TestHelpers
{
    internal class DatabaseManager
    {
        /// <summary>
        /// Creates if necessary, and runs migrations on both a transient and persistent database
        /// </summary>
        public static void MigrateDatabases()
        {
            var transientContext = DependencyInjection.DependencyInjection.Provider.GetService<SearchAlgorithmContext>();
            var persistentContext = DependencyInjection.DependencyInjection.Provider.GetService<SearchAlgorithmPersistentContext>();

            var connectionStringB =
                DependencyInjection.DependencyInjection.Provider.GetService<IConfiguration>().GetSection("ConnectionStrings")["SqlB"];
            var transientContextB = new ContextFactory().Create(connectionStringB);

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
            ClearTransientDatabases();
            ClearPersistentDatabase();
        }

        /// <summary>
        /// Clearing the persistent database will clear all refresh records, and there will no longer be an active HLA nomenclature version
        /// Of you call this anywhere other than global setup, you must re-add a data refresh record to prevent search tests failing due to this.
        /// </summary>
        private static void ClearPersistentDatabase()
        {
            var persistentContext = DependencyInjection.DependencyInjection.Provider.GetService<SearchAlgorithmPersistentContext>();
            Task.WaitAll(ClearPersistentDatabase(persistentContext));
        }
        
        public static void ClearTransientDatabases()
        {
            var connStringFactory = DependencyInjection.DependencyInjection.Provider.GetService<StaticallyChosenTransientSqlConnectionStringProviderFactory>();
            var connStringA = connStringFactory.GenerateConnectionStringProvider(TransientDatabase.DatabaseA).GetConnectionString();
            var connStringB = connStringFactory.GenerateConnectionStringProvider(TransientDatabase.DatabaseB).GetConnectionString();

            var transientContextA = new ContextFactory().Create(connStringA);
            var transientContextB = new ContextFactory().Create(connStringB);
            Task.WaitAll(
                ClearTransientDatabase(transientContextA),
                ClearTransientDatabase(transientContextB)
            );
        }

        private static Task ClearTransientDatabase(DbContext context)
        {
            if (context != null)
            {
                return context.Database.ExecuteSqlRawAsync(@"
                DELETE FROM [HlaNamePGroupRelationAtA]
                DELETE FROM [HlaNamePGroupRelationAtB]
                DELETE FROM [HlaNamePGroupRelationAtC]
                DELETE FROM [HlaNamePGroupRelationAtDrb1]
                DELETE FROM [HlaNamePGroupRelationAtDqb1]
                
                DELETE FROM [PGroupNames]
                DELETE FROM [HlaNames]
                
                DELETE FROM [Donors]
                DELETE FROM [MatchingHlaAtA]
                DELETE FROM [MatchingHlaAtB]
                DELETE FROM [MatchingHlaAtC]
                DELETE FROM [MatchingHlaAtDrb1]
                DELETE FROM [MatchingHlaAtDqb1]
                ");
            }
            return Task.CompletedTask;
        }

        private static Task ClearPersistentDatabase(SearchAlgorithmPersistentContext context)
        {
            return context.Database.ExecuteSqlRawAsync($"TRUNCATE TABLE {DataRefreshRecord.QualifiedTableName}");
        }
    }
}