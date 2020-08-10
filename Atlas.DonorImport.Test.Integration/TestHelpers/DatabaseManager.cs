using System;
using Atlas.DonorImport.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.DonorImport.Test.Integration.TestHelpers
{
    public static class DatabaseManager
    {
        /// <summary>
        /// Creates if necessary, and runs migrations on both a transient and persistent database
        /// </summary>
        public static void SetupDatabase()
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
        public static void ClearDatabases()
        {
            var context = DependencyInjection.DependencyInjection.Provider.GetService<DonorContext>();
            context?.Database.ExecuteSqlRaw("TRUNCATE TABLE [Donors]");
            context?.Database.ExecuteSqlRaw("TRUNCATE TABLE [DonorImportHistory]");
            context?.Database.ExecuteSqlRaw("TRUNCATE TABLE [DonorLogs]");
        }
    }
}