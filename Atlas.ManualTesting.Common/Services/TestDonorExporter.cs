using Atlas.DonorImport.Data.Repositories;
using Atlas.DonorImport.Data.Models;
using MoreLinq;

namespace Atlas.ManualTesting.Common.Services
{
    public interface ITestDonorExporter
    {
        Task ExportDonorsToDonorStore(IEnumerable<Donor> donors);
    }

    public class TestDonorExporter : ITestDonorExporter
    {
        private readonly IDonorReadRepository readRepository;
        private readonly IDonorImportRepository importRepository;

        public TestDonorExporter(IDonorReadRepository readRepository, IDonorImportRepository importRepository)
        {
            this.readRepository = readRepository;
            this.importRepository = importRepository;
        }

        public async Task ExportDonorsToDonorStore(IEnumerable<Donor> donors)
        {
            System.Diagnostics.Debug.WriteLine("Deleting all existing donors from donor store.");
            await DeleteExistingDonorsFromDonorImportRepo();

            System.Diagnostics.Debug.WriteLine("Inserting test donors into donor store.");
            await InsertTestDonorsIntoDonorImportRepo(donors);
        }

        private async Task DeleteExistingDonorsFromDonorImportRepo()
        {
            const int batchSize = 1000;
            var batchedIds = readRepository.StreamAllDonors()
                .Select(d => d.AtlasId)
                .Batch(batchSize);

            foreach (var batch in batchedIds)
            {
                await importRepository.DeleteDonorBatch(batch.ToList());
            }
        }

        private async Task InsertTestDonorsIntoDonorImportRepo(IEnumerable<Donor> donors)
        {
            await importRepository.InsertDonorBatch(donors);
        }
    }
}