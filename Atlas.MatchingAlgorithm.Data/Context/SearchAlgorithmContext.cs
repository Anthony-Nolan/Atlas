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
                .HasDefaultValue(true)
                .HasSentinel(false);

            modelBuilder.Entity<DonorManagementLog>()
                .HasIndex(d => d.DonorId)
                .IsUnique();

            modelBuilder.Entity<DonorManagementLog>()
                .HasIndex(d => d.LastUpdateDateTime)
                .IncludeProperties(x => x.DonorId);

            // Note: three indexes below share DonorId as their key column, so each one passes a name to
            // HasIndex. Unnamed HasIndex calls on the same property configure ONE index rather than several -
            // this is documented EF Core behaviour, not a bug - so before ATL-310 the unnamed DQB1 call
            // overwrote the unnamed Locus C call, and FI_DonorIdsWithoutLocusC was silently absent from the
            // model while still existing in the database. The name is also used as the database name, so
            // HasName/HasDatabaseName is not needed on top.
            modelBuilder.Entity<Donor>()
                .HasIndex(d => d.DonorId, "FI_DonorIdsWithoutLocusC")
                .IncludeProperties(d => new { d.C_1, d.C_2 })
                .HasFilter("[C_1] IS NULL AND [C_2] IS NULL");

            modelBuilder.Entity<Donor>()
                .HasIndex(d => d.DonorId, "FI_DonorIdsWithoutLocusDQB1")
                .IncludeProperties(d => new { d.DQB1_1, d.DQB1_2 })
                .HasFilter("[DQB1_1] IS NULL AND [DQB1_2] IS NULL");

            // ATL-310: the donor join in DonorSearchRepository.MatchAtLocusSql needs DonorId, DonorType and
            // RegistryCode together. Without the included columns below, SQL Server seeks IX_DonorId and then
            // does a clustered-index lookup for every candidate donor row - two random reads per row into a
            // table that is tens of GB at live donor volumes, which saturates read IO and times searches out.
            modelBuilder.Entity<Donor>()
                .HasIndex(d => d.DonorId, "IX_DonorId")
                .IncludeProperties(d => new { d.DonorType, d.RegistryCode });

            modelBuilder.Entity<Donor>().HasIndex(d => d.ExternalDonorCode);

            // Covers searches that pass no registry codes, where the join needs DonorType only.
            modelBuilder.Entity<Donor>()
                .HasIndex(d => d.DonorType, "IX_DonorType__DonorId")
                .IncludeProperties(d => d.DonorId);

            modelBuilder.Entity<Donor>()
                .HasIndex(d => new { d.DonorType, d.RegistryCode })
                .IncludeProperties(d => d.DonorId);

            // Persist AllowedLociKey as its member name rather than its int value, so the stored table data is
            // self-describing without needing to cross-reference the enum in code. Longest name is 11 chars.
            modelBuilder.Entity<DonorSubjectGenotypeSet>()
                .Property(x => x.AllowedLociKey)
                .HasConversion<string>()
                .HasMaxLength(16);

            modelBuilder.Entity<SubjectGenotypeSetValue>()
                .Property(x => x.AllowedLociKey)
                .HasConversion<string>()
                .HasMaxLength(16);

            modelBuilder.Entity<DonorSubjectGenotypeSet>()
                .HasIndex(x => new { x.DonorId, x.AllowedLociKey })
                .IsUnique();

            modelBuilder.Entity<SubjectGenotypeSetValue>()
                .HasIndex(x => new { x.HlaTypingKey, x.HaplotypeFrequencySetId, x.AllowedLociKey })
                .IsUnique();

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

        public DbSet<SubjectGenotypeSetValue> SubjectGenotypeSetValues { get; set; }
        public DbSet<DonorSubjectGenotypeSet> DonorSubjectGenotypeSets { get; set; }

        public DbSet<PGroupName> PGroupNames { get; set; }
        public DbSet<HlaName> HlaNames { get; set; }
    }
}
