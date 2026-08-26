using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Atlas.MatchingAlgorithm.Data.Context
{
    public class ContextFactory : IDesignTimeDbContextFactory<SearchAlgorithmContext>
    {
        private const int MigrationCommandTimeoutInSeconds = 3600;

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
                    // build an index over the Donors table at live donor volumes.
                    //
                    // The release does not use this timeout today. build-pipeline.yml generates an idempotent SQL
                    // script with dotnet ef migrations script, and a release task applies that script, so the
                    // timeout that governs there belongs to the task and not to EF.
                    //
                    // This timeout does govern every path that applies migrations through the EF runtime instead:
                    // dotnet ef database update run by hand against a full-size database, a migration bundle, or
                    // Database.Migrate() in the integration and validation test projects. Keep it, so that a move
                    // away from generated scripts cannot make a long index build fail part way through.
                    builder.CommandTimeout(MigrationCommandTimeoutInSeconds);
                }
            );

            return new SearchAlgorithmContext(optionsBuilder.Options);
        }
    }
}