using System;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Reflection;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Data.Entity;
using Nova.SearchAlgorithm.Data.Exceptions;
using Nova.Utils.ApplicationInsights;
using Nova.Utils.Entity;

namespace Nova.SearchAlgorithm.Data
{
    public interface ISearchAlgorithmContext : IDisposable
    {
        DbSet<Donor> Donors { get; set; }
        DbSet<PGroupName> PGroupNames { get; set; }
        DbSet<MatchingHlaAtA> MatchingHlaAtA { get; set; }
        DbSet<MatchingHlaAtB> MatchingHlaAtB { get; set; }
        DbSet<MatchingHlaAtC> MatchingHlaAtC { get; set; }
        DbSet<MatchingHlaAtDrb1> MatchingHlaAtDrb1 { get; set; }
        DbSet<MatchingHlaAtDqb1> MatchingHlaAtDqb1 { get; set; }
        DbSet MatchingHlasAtLocus(Locus locus);
    }
    
    // We should only use entity framework for maintaining the database schema, and for test data
    // In all other cases we should use Dapper within repositories, else we won't be able to switch between databases at runtime
    public class SearchAlgorithmContext : NovaDbContext, ISearchAlgorithmContext
    {
        private const string ConnectionStringName = "SqlConnectionString";

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

            modelBuilder.Conventions.Remove<OneToManyCascadeDeleteConvention>();
            modelBuilder.Conventions.Remove<ManyToManyCascadeDeleteConvention>();
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

        public DbSet MatchingHlasAtLocus(Locus locus)
        {
            switch (locus)
            {
                case Locus.A:
                    return MatchingHlaAtA;
                case Locus.B:
                    return MatchingHlaAtB;
                case Locus.C:
                    return MatchingHlaAtC;
                case Locus.Dqb1:
                    return MatchingHlaAtDqb1;
                case Locus.Drb1:
                    return MatchingHlaAtDrb1;
                default:
                    throw new DataHttpException($"Could not select DBSet for unknown locus {locus}");
            }
        }
    }
}
