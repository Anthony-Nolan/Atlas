using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Atlas.DonorImport.Data.Models;
using Atlas.DonorImport.Data.Repositories;
using Atlas.MatchPrediction.Test.Verification.Data.Models.TestHarness;
using Atlas.MatchPrediction.Test.Verification.Data.Repositories;
using MoreLinq;

namespace Atlas.MatchPrediction.Test.Verification.Services
{
    internal interface ITestDonorExporter
    {
        /// <summary>
        /// Exports genotypes and phenotypes of test donors to the Atlas donor store.
        ///
        /// Note: donors are not assigned a registry or ethnicity code so that
        /// the MPA is forced to refer to the global haplotype frequency set when calculating
        /// match predictions for potential matches.
        /// </summary>
        Task ExportDonorsToDonorStore(int testHarnessId);
    }

    internal class TestDonorExporter : ITestDonorExporter
    {
        private readonly IDonorReadRepository readRepository;
        private readonly IDonorImportRepository importRepository;
        private readonly ISimulantsRepository simulantsRepository;

        public TestDonorExporter(
            IDonorReadRepository readRepository, 
            IDonorImportRepository importRepository,
            ISimulantsRepository simulantsRepository)
        {
            this.readRepository = readRepository;
            this.importRepository = importRepository;
            this.simulantsRepository = simulantsRepository;
        }

        public async Task ExportDonorsToDonorStore(int testHarnessId)
        {
            Debug.WriteLine("Deleting all existing donors from donor store.");
            await DeleteExistingDonorsFromDonorImportRepo();

            Debug.WriteLine("Inserting test donors into donor store.");
            await InsertTestDonorsIntoDonorImportRepo(testHarnessId);
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

        private async Task InsertTestDonorsIntoDonorImportRepo(int testHarnessId)
        {
            var donors = (await simulantsRepository.GetSimulants(testHarnessId, TestIndividualCategory.Donor.ToString()))
                .Select(MapToDonorImportModel);
            await importRepository.InsertDonorBatch(donors);
        }

        private static Donor MapToDonorImportModel(Simulant simulant)
        {
            const string updateFileText = "UploadedDirectlyForMPAVerification";

            var donor = new Donor
            {
                ExternalDonorCode = simulant.Id.ToString(),
                UpdateFile = updateFileText,
                LastUpdated = DateTimeOffset.UtcNow,
                DonorType = DatabaseDonorType.Cord,
                EthnicityCode = null,
                RegistryCode = null,
                A_1 = simulant.A_1,
                A_2 = simulant.A_2,
                B_1 = simulant.B_1,
                B_2 = simulant.B_2,
                C_1 = simulant.C_1,
                C_2 = simulant.C_2,
                DQB1_2 = simulant.DQB1_1,
                DQB1_1 = simulant.DQB1_2,
                DRB1_1 = simulant.DRB1_1,
                DRB1_2 = simulant.DRB1_2
            };

            // The hash is not needed for verification, but no harm in calculating it
            // in case donors are also imported into the test environment via the normal
            // donor import file route.
            donor.CalculateHash();

            return donor;
        }
    }
}
