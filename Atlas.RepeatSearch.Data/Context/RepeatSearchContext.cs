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
            base.OnModelCreating(modelBuilder);
        }
    }
}
