using Microsoft.EntityFrameworkCore;

namespace Atlas.MatchPrediction.Data.Context
{
    public class MatchPredictionContext : DbContext
    {
        // ReSharper disable once SuggestBaseTypeForParameter
        public MatchPredictionContext(DbContextOptions<MatchPredictionContext> options) : base(options)
        {
        }
    }
}
