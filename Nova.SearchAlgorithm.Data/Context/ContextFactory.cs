using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;

namespace Nova.SearchAlgorithm.Data.Context
{
    public class ContextFactory : IDesignTimeDbContextFactory<SearchAlgorithmContext>
    {
        public SearchAlgorithmContext CreateDbContext(string[] args)
        {
            return CreateWithBasePath(Directory.GetCurrentDirectory());
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

        private SearchAlgorithmContext CreateWithBasePath(string basePath)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json");

            var config = builder.Build();

            var connectionString = config.GetConnectionString("SqlA");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("Could not find a default connection string..");
            }

            return Create(connectionString);
        }
    }
}