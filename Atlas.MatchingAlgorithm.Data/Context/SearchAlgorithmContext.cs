using System.Linq;
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Atlas.MatchingAlgorithm.Data.Context
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

            modelBuilder.Entity<Donor>()
                .Property(d => d.IsAvailableForSearch)
                .HasDefaultValue(true);

            modelBuilder.Entity<DonorManagementLog>()
                .HasIndex(d => d.DonorId)
                .IsUnique();

            modelBuilder.Entity<DonorManagementLog>()
                .HasIndex(d => d.LastUpdateDateTime)
                .IncludeProperties(x => x.DonorId);

            // Note: The Model Builder seems to have a bug
            // where it will drop the Locus C filtered index
            // when generating the DQB1 filtered index.
            // Ensure that the migrations are created correctly
            // when making changes to the following index definitions,
            // or if adding new filtered indexes to the same table.
            modelBuilder.Entity<Donor>()
                .HasIndex(d => d.DonorId)
                .IncludeProperties(d => new { d.C_1, d.C_2 })
                .HasFilter("[C_1] IS NULL AND [C_2] IS NULL")
                .HasName("FI_DonorIdsWithoutLocusC");

            modelBuilder.Entity<Donor>()
                .HasIndex(d => d.DonorId)
                .IncludeProperties(d => new { d.DQB1_1, d.DQB1_2 })
                .HasFilter("[DQB1_1] IS NULL AND [DQB1_2] IS NULL")
                .HasName("FI_DonorIdsWithoutLocusDQB1");

            modelBuilder.Entity<Donor>().HasIndex(d => d.ExternalDonorCode);

            base.OnModelCreating(modelBuilder);
        }

        public DbSet<Donor> Donors { get; set; }

        public DbSet<MatchingHlaAtA> MatchingHlaAtA { get; set; }
        public DbSet<MatchingHlaAtB> MatchingHlaAtB { get; set; }
        public DbSet<MatchingHlaAtC> MatchingHlaAtC { get; set; }
        public DbSet<MatchingHlaAtDrb1> MatchingHlaAtDrb1 { get; set; }
        public DbSet<MatchingHlaAtDqb1> MatchingHlaAtDqb1 { get; set; }

        public DbSet<HlaNamePGroupRelationAtA> HlaNamePGroupRelationsAtA { get; set; }
        public DbSet<HlaNamePGroupRelationAtB> HlaNamePGroupRelationAtB { get; set; }
        public DbSet<HlaNamePGroupRelationAtC> HlaNamePGroupRelationAtC { get; set; }
        public DbSet<HlaNamePGroupRelationAtDrb1> HlaNamePGroupRelationAtDrb1 { get; set; }
        public DbSet<HlaNamePGroupRelationAtDqb1> HlaNamePGroupRelationAtDqb1 { get; set; }

        public DbSet<DonorManagementLog> DonorManagementLogs { get; set; }

        public DbSet<PGroupName> PGroupNames { get; set; }
        public DbSet<HlaName> HlaNames { get; set; }
    }
}
