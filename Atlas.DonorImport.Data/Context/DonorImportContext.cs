using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Atlas.MatchingAlgorithm.Data.Context
{
    public class DonorImportContext : DbContext
    {
        // ReSharper disable once SuggestBaseTypeForParameter
        public DonorImportContext(DbContextOptions<DonorImportContext> options) : base(options)
        {
        }
    }
}
