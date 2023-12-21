using Atlas.ManualTesting.Common.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Atlas.ManualTesting.Common.Contexts
{
    public interface IDonorExportData
    {
        public DbSet<TestDonorExportRecord> TestDonorExportRecords { get; set; }
    }
}