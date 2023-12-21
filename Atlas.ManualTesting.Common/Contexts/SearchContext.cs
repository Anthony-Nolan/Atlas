using Atlas.ManualTesting.Common.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Atlas.ManualTesting.Common.Contexts
{
    public interface ISearchData<TSearch, TMatchedDonor>
        where TSearch : SearchRequestRecord
        where TMatchedDonor : MatchedDonorBase
    {
        public DbSet<TSearch> SearchRequests { get; set; }
        public DbSet<TMatchedDonor> MatchedDonors { get; set; }
        public DbSet<LocusMatchCount> MatchCounts { get; set; }
        public DbSet<MatchProbability> MatchProbabilities { get; set; }
    }
}