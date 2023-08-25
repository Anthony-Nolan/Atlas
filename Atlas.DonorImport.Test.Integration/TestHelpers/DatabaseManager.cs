using System;
using Atlas.DonorImport.Data.Context;
using Atlas.DonorImport.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.DonorImport.Test.Integration.TestHelpers
{
    internal static class DatabaseManager
    {
        /// <summary>
        /// Creates if necessary, and runs migrations on both a transient and persistent database
        /// </summary>
        internal static void SetupDatabase()
        {
            var context = DependencyInjection.DependencyInjection.Provider.GetService<DonorContext>();

            if (context == null)
            {
                throw new Exception("Database context could resolved - DI has not been correctly configured.");
            }
            
            context.Database.Migrate();
        }

        /// <summary>
        /// Clears the test database of data. Can be accessed by fixtures to run after each fixture, but not after each test.
        /// </summary>
        internal static void ClearDatabases()
        {
            var context = DependencyInjection.DependencyInjection.Provider.GetService<DonorContext>();
            context?.Database.ExecuteSqlRaw($"TRUNCATE TABLE {Donor.QualifiedTableName}");
            context?.Database.ExecuteSqlRaw($"TRUNCATE TABLE {DonorImportHistoryRecord.QualifiedTableName}");
            context?.Database.ExecuteSqlRaw($"TRUNCATE TABLE {DonorLog.QualifiedTableName}");
            context?.Database.ExecuteSqlRaw($"TRUNCATE TABLE {PublishableDonorUpdate.QualifiedTableName}");
            context?.Database.ExecuteSqlRaw($"TRUNCATE TABLE {DonorImportFailure.QualifiedTableName}");
        }

        internal static void ClearPublishableDonorUpdates()
        {
            var context = DependencyInjection.DependencyInjection.Provider.GetService<DonorContext>();
            context?.Database.ExecuteSqlRaw($"TRUNCATE TABLE {PublishableDonorUpdate.QualifiedTableName}");
        }
    }
}