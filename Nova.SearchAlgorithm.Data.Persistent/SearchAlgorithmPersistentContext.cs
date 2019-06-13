using System;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Reflection;
using Nova.SearchAlgorithm.Data.Persistent.Models;
using Nova.SearchAlgorithm.Data.Persistent.Models.ScoringWeightings;
using Nova.Utils.ApplicationInsights;
using Nova.Utils.Entity;

namespace Nova.SearchAlgorithm.Data.Persistent
{
    public interface ISearchAlgorithmPersistentContext : IDisposable
    {
        DbSet<GradeWeighting> GradeWeightings { get; set; }
        DbSet<ConfidenceWeighting> ConfidenceWeightings { get; set; }
    }

    public class SearchAlgorithmPersistentContext : NovaDbContext, ISearchAlgorithmPersistentContext
    {
        private const string ConnectionStringName = "PersistentSqlConnectionString";

        public SearchAlgorithmPersistentContext() : this((IEntityLogger)null)
        {
        }

        public SearchAlgorithmPersistentContext(IEntityLogger logger) : base(ConnectionStringName, logger)
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
        
        public DbSet<GradeWeighting> GradeWeightings { get; set; }
        public DbSet<ConfidenceWeighting> ConfidenceWeightings { get; set; }
        public DbSet<DataRefreshRecord> DataRefreshRecords { get; set; }
    }
}
