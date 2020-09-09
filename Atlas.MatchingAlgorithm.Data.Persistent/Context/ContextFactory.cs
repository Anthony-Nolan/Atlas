using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Atlas.MatchingAlgorithm.Data.Persistent.Context
{
    public class ContextFactory : IDesignTimeDbContextFactory<SearchAlgorithmPersistentContext>
    {
        // This method is called by entity framework to create a context when generating/running migrations
        public SearchAlgorithmPersistentContext CreateDbContext(string[] args)
        {
            var basePath = Directory.GetCurrentDirectory();
            var builder = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json");

            var config = builder.Build();

            // This is the default connection string to be used when running migrations locally
            // When running, the connection string should be passed manually into the Create method in this class.
            var connectionString = config.GetConnectionString("PersistentSql");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException($"Could not find a default connection string. Note: config basePath was: {basePath}.");
            }

            return Create(connectionString);
        }

        public SearchAlgorithmPersistentContext Create(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentException($"{nameof(connectionString)} is null or empty.", nameof(connectionString));
            }

            var optionsBuilder = new DbContextOptionsBuilder<SearchAlgorithmPersistentContext>();

            optionsBuilder.UseSqlServer(connectionString, builder =>
            {
                builder.EnableRetryOnFailure(5, TimeSpan.FromSeconds(5), null);
                builder.MigrationsHistoryTable("_EFMigrationsHistory", SearchAlgorithmPersistentContext.Schema);
            });

            return new SearchAlgorithmPersistentContext(optionsBuilder.Options);
        }
    }
}