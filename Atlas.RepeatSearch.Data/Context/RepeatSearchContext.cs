using Atlas.RepeatSearch.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Atlas.RepeatSearch.Data.Context
{
    public class RepeatSearchContext : DbContext
    {
        internal const string Schema = "RepeatSearch";
        
        // ReSharper disable once SuggestBaseTypeForParameter
        public RepeatSearchContext(DbContextOptions<RepeatSearchContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema(Schema);
            modelBuilder.Entity<CanonicalResultSet>().SetUpResultSetModel();
            modelBuilder.Entity<SearchResult>().SetUpSearchResultModel();
            base.OnModelCreating(modelBuilder);
        }
        
        public DbSet<CanonicalResultSet> CanonicalResultSets { get; set; }
    }
}
