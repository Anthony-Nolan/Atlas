using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;

namespace Atlas.MatchingAlgorithm.Data.Context
{
    public class ContextFactory : IDesignTimeDbContextFactory<SearchAlgorithmContext>
    {
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
                throw new InvalidOperationException("Could not find a default connection string..");
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

            optionsBuilder.UseSqlServer(connectionString);

            return new SearchAlgorithmContext(optionsBuilder.Options);
        }
    }
}