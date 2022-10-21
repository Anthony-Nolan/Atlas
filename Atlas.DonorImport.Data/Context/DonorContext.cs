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
            modelBuilder.Entity<Donor>().SetUpDonorModel();
            modelBuilder.Entity<DonorImportHistoryRecord>().SetUpDonorImportHistory();
            modelBuilder.Entity<DonorLog>().SetUpDonorLogModel();
            modelBuilder.Entity<PublishableDonorUpdate>();
            modelBuilder.HasDefaultSchema("Donors");
        }

        public DbSet<Donor> Donors { get; set; }
    }
}
