using Atlas.RepeatSearch.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;

namespace Atlas.RepeatSearch.Test.Integration
{
    [SetUpFixture]
    public class IntegrationTestSetUp
    {
        /// <summary>
        /// From appsettings.json, or from the environment variable <c>ConnectionStrings__RepeatSearchSql</c> (build pipeline).
        /// </summary>
        public static string ConnectionString { get; private set; }

        [OneTimeSetUp]
        public void SetUp()
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables()
                .Build();

            ConnectionString = configuration.GetConnectionString("RepeatSearchSql");

            using var context = new ContextFactory().Create(ConnectionString);
            context.Database.Migrate();
        }
    }
}
