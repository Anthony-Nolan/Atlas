using Microsoft.EntityFrameworkCore;

namespace Atlas.DonorImport.Data.Context
{
    public class DonorImportContext : DbContext
    {
        // ReSharper disable once SuggestBaseTypeForParameter
        public DonorImportContext(DbContextOptions<DonorImportContext> options) : base(options)
        {
        }
    }
}
