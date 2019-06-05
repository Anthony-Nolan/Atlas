using System.Data.Entity.Migrations;
using Microsoft.EntityFrameworkCore;
using Nova.SearchAlgorithm.Data;
using Nova.SearchAlgorithm.Data.Persistent;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.Integration
{
    [SetUpFixture]
    public class SetUp
    {
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            SetupPersistentDatabase();
            SetupDatabase();
        }

        private static void SetupPersistentDatabase()
        {
            using (var context = new SearchAlgorithmPersistentContext(new DbContextOptions<SearchAlgorithmPersistentContext>()))
            {
                context.Database.CreateIfNotExists();
                var config = new Data.Persistent.Migrations.Configuration();
                var migrator = new DbMigrator(config);
                migrator.Update();
            }
        }

        private static void SetupDatabase()
        {
            using (var context = new SearchAlgorithmContext())
            {
                context.Database.CreateIfNotExists();
                var config = new Data.Migrations.Configuration();
                var migrator = new DbMigrator(config);
                migrator.Update();
            }
        }
    }
}