using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Atlas.MatchingAlgorithm.Data.Context
{
    public class ContextFactory : IDesignTimeDbContextFactory<SearchAlgorithmContext>
    {
        private const int migrationCommandTimeoutInSeconds = 3600;


        // This method is called by entity framework to create a context when generating/running migrations
        public SearchAlgorithmContext CreateDbContext(string[] args)
        {
            var basePath = Directory.GetCurrentDirectory();
            var builder = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json");

            var config = builder.Build();

            // This is the default connection string to be used when running migrations locally
            // When running, the connection string should be passed manually into the Create method in this class.
            var connectionString = config.GetConnectionString("Sql");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException($"Could not find a default connection string. Note: config basePath was: {basePath}.");
            }

            return Create(connectionString);
        }

        public SearchAlgorithmContext Create(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentException($"{nameof(connectionString)} is null or empty.", nameof(connectionString));
            }

            var optionsBuilder = new DbContextOptionsBuilder<SearchAlgorithmContext>();

            optionsBuilder.UseSqlServer(connectionString, builder =>
            {
                builder.EnableRetryOnFailure(5, TimeSpan.FromSeconds(5), null);

                // This context is used for migrations and for seeding test data, never for the Dapper query
                // paths that serve search. The default command timeout is 30 seconds, which is not enough to
                // build an index over the Donors table at live donor volumes, so a migration that rebuilds one
                // would fail part way through.
                builder.CommandTimeout(migrationCommandTimeoutInSeconds);
            });

            return new SearchAlgorithmContext(optionsBuilder.Options);
        }
    }
}