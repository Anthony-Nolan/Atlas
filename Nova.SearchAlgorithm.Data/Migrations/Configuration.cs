using System.Data.Entity.Migrations;

namespace Nova.SearchAlgorithm.Data.Migrations
{
    public sealed class Configuration : DbMigrationsConfiguration<SearchAlgorithmContext>
    {
        private const int DefaultWeight = 0;

        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
            ContextKey = "Nova.SearchAlgorithm.Data.SearchAlgorithmContext";
            CommandTimeout = 0;
        }
    }
}