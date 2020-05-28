using Atlas.MatchPrediction.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Atlas.MatchPrediction.Test.Integration.TestHelpers
{
    internal static class DatabaseManager
    {
        /// <summary>
        /// Creates if necessary, and runs migrations on both a transient and persistent database
        /// </summary>
        public static void SetupDatabase()
        {
            var context = DependencyInjection.DependencyInjection.Provider.GetService<MatchPredictionContext>();

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
            var context = DependencyInjection.DependencyInjection.Provider.GetService<MatchPredictionContext>();

            context?.Database.ExecuteSqlRaw($"TRUNCATE TABLE [{nameof(context.HaplotypeFrequencies)}]");
            context?.Database.ExecuteSqlRaw($"DELETE FROM [{nameof(context.HaplotypeFrequencySets)}]");
        }
    }
}