using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.DonorImport.Data.Models;
using Atlas.ManualTesting.Common.Models;
using Atlas.ManualTesting.Common.Repositories;
using Atlas.ManualTesting.Common.Services;
using Atlas.MatchPrediction.Test.Verification.Data.Models.Entities.TestHarness;
using Atlas.MatchPrediction.Test.Verification.Data.Repositories;
using Atlas.MatchPrediction.Test.Verification.Models;

namespace Atlas.MatchPrediction.Test.Verification.Services
{
    public interface IVerificationAtlasPreparer : IAtlasPreparer
    {
        Task PrepareAtlasWithTestHarnessDonors(int testHarnessId);
    }

    internal class VerificationAtlasPreparer : AtlasPreparer, IVerificationAtlasPreparer
    {
        private readonly ISimulantsRepository simulantsRepository;
        private readonly ITestHarnessRepository testHarnessRepository;
        private int harnessId;

        public VerificationAtlasPreparer(
            ISimulantsRepository simulantsRepository,
            ITestHarnessRepository testHarnessRepository,
            ITestDonorExporter testDonorExporter,
            ITestDonorExportRepository exportRepository,
            string dataRefreshRequestUrl)
            : base(testDonorExporter, exportRepository, dataRefreshRequestUrl)
        {
            this.simulantsRepository = simulantsRepository;
            this.testHarnessRepository = testHarnessRepository;
        }

        public async Task PrepareAtlasWithTestHarnessDonors(int testHarnessId)
        {
            harnessId = testHarnessId;
            var exportRecordId = await PrepareAtlasDonorStores();
            await testHarnessRepository.SetExportRecordId(testHarnessId, exportRecordId);
        }

        protected override async Task<IEnumerable<Donor>> GetTestDonors()
        {
            var donors = await simulantsRepository.GetSimulants(harnessId, TestIndividualCategory.Donor.ToString());
            return donors.Select(MapToDonorImportModel);
        }

        private static Donor MapToDonorImportModel(Simulant simulant)
        {
            const string updateFileText = "UploadedDirectlyForMPAVerification";

            var donor = new Donor
            {
                ExternalDonorCode = simulant.Id.ToString(),
                UpdateFile = updateFileText,
                LastUpdated = DateTimeOffset.UtcNow,
                DonorType = simulant.SimulatedHlaTypingCategory.ToDonorType().ToDatabaseType(),
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