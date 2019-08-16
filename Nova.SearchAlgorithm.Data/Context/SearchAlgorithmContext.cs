using System.Linq;
using Microsoft.EntityFrameworkCore;
using Nova.SearchAlgorithm.Data.Entity;
using Nova.SearchAlgorithm.Data.Models;

namespace Nova.SearchAlgorithm.Data
{
    // We should only use entity framework for maintaining the database schema, and for test data
    // In all other cases we should use Dapper within repositories, else we won't be able to switch between databases at runtime
    public class SearchAlgorithmContext : DbContext
    {
        // ReSharper disable once SuggestBaseTypeForParameter
        public SearchAlgorithmContext(DbContextOptions<SearchAlgorithmContext> options) : base(options)
        {
        }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            foreach (var relationship in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            {
                relationship.DeleteBehavior = DeleteBehavior.Restrict;
            }

            modelBuilder.Entity<DonorManagementLog>()
                .HasIndex(d => d.DonorId)
                .IsUnique();

            base.OnModelCreating(modelBuilder);
        }

        public DbSet<Donor> Donors { get; set; }
        public DbSet<MatchingHlaAtA> MatchingHlaAtA { get; set; }
        public DbSet<MatchingHlaAtB> MatchingHlaAtB { get; set; }
        public DbSet<MatchingHlaAtC> MatchingHlaAtC { get; set; }
        public DbSet<MatchingHlaAtDrb1> MatchingHlaAtDrb1 { get; set; }
        public DbSet<MatchingHlaAtDqb1> MatchingHlaAtDqb1 { get; set; }
        public DbSet<DonorManagementLog> DonorManagementLogs { get; set; }
    }
}
