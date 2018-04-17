namespace Nova.SearchService.Data.Migrations
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;

    internal sealed class Configuration : DbMigrationsConfiguration<Nova.SearchAlgorithm.Data.SearchAlgorithmContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = true;
            ContextKey = "Nova.SearchAlgorithm.Data.SearchAlgorithmContext";
        }

        protected override void Seed(Nova.SearchAlgorithm.Data.SearchAlgorithmContext context)
        {
            //  This method will be called after migrating to the latest version.

            //  You can use the DbSet<T>.AddOrUpdate() helper extension method 
            //  to avoid creating duplicate seed data.
        }
    }
}
