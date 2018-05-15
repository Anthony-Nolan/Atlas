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
        DbSet<MatchingGroupAtA> MatchingGroupsAtA { get; set; }
        DbSet<MatchingGroupAtB> MatchingGroupsAtB { get; set; }
        DbSet<MatchingGroupAtC> MatchingGroupsAtC { get; set; }
        DbSet<MatchingGroupAtDrb1> MatchingGroupsAtDrb1 { get; set; }
        DbSet<MatchingGroupAtDqb1> MatchingGroupsAtDqb1 { get; set; }
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
        public DbSet<MatchingGroupAtA> MatchingGroupsAtA { get; set; }
        public DbSet<MatchingGroupAtB> MatchingGroupsAtB { get; set; }
        public DbSet<MatchingGroupAtC> MatchingGroupsAtC { get; set; }
        public DbSet<MatchingGroupAtDrb1> MatchingGroupsAtDrb1 { get; set; }
        public DbSet<MatchingGroupAtDqb1> MatchingGroupsAtDqb1 { get; set; }
    }
}
