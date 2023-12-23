using Atlas.ManualTesting.Common.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Atlas.ManualTesting.Common.Contexts
{
    public interface ISearchData<TSearch>
        where TSearch : SearchRequestRecord
    {
        public DbSet<TSearch> SearchRequests { get; set; }
        public DbSet<MatchedDonor> MatchedDonors { get; set; }
        public DbSet<LocusMatchDetails> LocusMatchDetails { get; set; }
        public DbSet<MatchedDonorProbability> MatchProbabilities { get; set; }
    }
}