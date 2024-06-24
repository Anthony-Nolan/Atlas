using Atlas.SearchTracking.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Atlas.SearchTracking.Data.Context
{
    public interface ISearchTrackingContext : IDisposable
    {
        public DbSet<SearchRequest> SearchRequests { get; }

        public DbSet<SearchRequestMatchingAlgorithmAttemptTiming> SearchRequestMatchingAlgorithmAttemptTimings { get; }

        public DbSet<SearchRequestMatchPredictionTiming> SearchRequestMatchPredictionTimings { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
    public class SearchTrackingContext : DbContext, ISearchTrackingContext
    {
        internal const string Schema = "SearchTracking";

        // ReSharper disable once SuggestBaseTypeForParameter
        public SearchTrackingContext(DbContextOptions<SearchTrackingContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema(Schema);

            modelBuilder.Entity<SearchRequestMatchingAlgorithmAttemptTiming>().SetUpModel();
            modelBuilder.Entity<SearchRequest>().SetUpModel();

            modelBuilder.Entity<SearchRequest>()
                .HasOne(x => x.SearchRequestMatchPredictionTiming)
                .WithOne(x => x.SearchRequest)
                .HasForeignKey<SearchRequestMatchPredictionTiming>(x => x.SearchRequestId);

            modelBuilder.Entity<SearchRequest>()
                .HasMany(x => x.SearchRequestMatchingAlgorithmAttemptTimings)
                .WithOne(x => x.SearchRequest)
                .HasForeignKey(x => x.SearchRequestId);


            base.OnModelCreating(modelBuilder);
        }

        public DbSet<SearchRequest> SearchRequests { get; set; }

        public DbSet<SearchRequestMatchingAlgorithmAttemptTiming> SearchRequestMatchingAlgorithmAttemptTimings { get; set; }

        public DbSet<SearchRequestMatchPredictionTiming> SearchRequestMatchPredictionTimings { get; set; }
    }
}
