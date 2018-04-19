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
        DbSet<DonorHla> DonorHla { get; set; }
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
        public DbSet<DonorHla> DonorHla { get; set; }
    }
}
