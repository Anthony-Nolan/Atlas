using Atlas.DonorImport.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Atlas.DonorImport.Data.Context
{
    public class DonorContext : DbContext
    {
        internal const string Schema = "Donors";
        
        // ReSharper disable once SuggestBaseTypeForParameter
        public DonorContext(DbContextOptions<DonorContext> options) : base(options)
        {       
        }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Donor>().SetUpModel();
            modelBuilder.Entity<DonorImportHistoryRecord>().SetUpModel();
            modelBuilder.Entity<DonorLog>().SetUpModel();
            modelBuilder.Entity<PublishableDonorUpdate>().SetUpModel();
            modelBuilder.Entity<DonorImportFailure>().SetUpModel();
            modelBuilder.HasDefaultSchema("Donors");
        }

        public DbSet<Donor> Donors { get; set; }
    }
}
