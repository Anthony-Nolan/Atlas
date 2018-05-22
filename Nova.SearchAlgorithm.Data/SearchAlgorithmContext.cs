using System;
using System.Data.Entity;
using System.Reflection;
using Nova.SearchAlgorithm.Data.Entity;
using Nova.Utils.ApplicationInsights;
using Nova.Utils.Entity;

namespace Nova.SearchAlgorithm.Data
{
    public interface ISearchServiceContext : IDisposable
    {
        DbSet<Donor> Donors { get; set; }
        DbSet<PGroupName> PGroupNames { get; set; }
        DbSet<MatchingHlaAtA> MatchingHlaAtA { get; set; }
        DbSet<MatchingHlaAtB> MatchingHlaAtB { get; set; }
        DbSet<MatchingHlaAtC> MatchingHlaAtC { get; set; }
        DbSet<MatchingHlaAtDrb1> MatchingHlaAtDrb1 { get; set; }
        DbSet<MatchingHlaAtDqb1> MatchingHlaAtDqb1 { get; set; }
    }

    public class SearchAlgorithmContext : NovaDbContext, ISearchServiceContext
    {
        public const string ConnectionStringName = "SqlConnectionString";

        public SearchAlgorithmContext() : this((IEntityLogger)null)
        {
        }

        public SearchAlgorithmContext(IEntityLogger logger) : base(ConnectionStringName, logger)
        {
            var type = typeof(System.Data.Entity.SqlServer.SqlProviderServices);
            if (type == null)
            {
                throw new Exception("Do not remove, ensures static reference to System.Data.Entity.SqlServer");
            }
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            var assembly = Assembly.GetExecutingAssembly();

            modelBuilder.Configurations.AddFromAssembly(assembly);
            base.OnModelCreating(modelBuilder);
        }

        public DbSet<Donor> Donors { get; set; }
        public DbSet<PGroupName> PGroupNames { get; set; }
        public DbSet<MatchingHlaAtA> MatchingHlaAtA { get; set; }
        public DbSet<MatchingHlaAtB> MatchingHlaAtB { get; set; }
        public DbSet<MatchingHlaAtC> MatchingHlaAtC { get; set; }
        public DbSet<MatchingHlaAtDrb1> MatchingHlaAtDrb1 { get; set; }
        public DbSet<MatchingHlaAtDqb1> MatchingHlaAtDqb1 { get; set; }
    }
}
