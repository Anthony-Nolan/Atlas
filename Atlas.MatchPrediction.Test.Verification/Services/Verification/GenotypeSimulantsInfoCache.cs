using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.MatchPrediction.Test.Verification.Data.Models;
using Atlas.MatchPrediction.Test.Verification.Data.Models.Entities.TestHarness;
using Atlas.MatchPrediction.Test.Verification.Data.Repositories;
using MoreLinq.Extensions;

namespace Atlas.MatchPrediction.Test.Verification.Services.Verification
{
    internal interface IGenotypeSimulantsInfoCache
    {
        Task<GenotypeSimulantsInfo> GetOrAddGenotypeSimulantsInfo(int verificationRunId);
        Task<IReadOnlyCollection<PatientDonorPair>> GetOrAddAllPossibleGenotypePatientDonorPairs(int verificationRunId);
    }

    internal class GenotypeSimulantsInfoCache : IGenotypeSimulantsInfoCache
    {
        private static readonly Dictionary<int, GenotypeSimulantsInfo> CachedGenotypeSimulantsInfo =
            new Dictionary<int, GenotypeSimulantsInfo>();
        private static readonly Dictionary<int, IReadOnlyCollection<PatientDonorPair>> CachedPossiblePdps =
            new Dictionary<int, IReadOnlyCollection<PatientDonorPair>>();

        private readonly IVerificationRunRepository verificationRunRepository;
        private readonly ISimulantsRepository simulantsRepository;

        public GenotypeSimulantsInfoCache(
            IVerificationRunRepository verificationRunRepository,
            ISimulantsRepository simulantsRepository)
        {
            this.verificationRunRepository = verificationRunRepository;
            this.simulantsRepository = simulantsRepository;
        }

        public async Task<GenotypeSimulantsInfo> GetOrAddGenotypeSimulantsInfo(int verificationRunId)
        {
            if (CachedGenotypeSimulantsInfo.ContainsKey(verificationRunId))
            {
                return CachedGenotypeSimulantsInfo[verificationRunId];
            }

            var verificationRun = await verificationRunRepository.GetVerificationRun(verificationRunId);
            var info = new GenotypeSimulantsInfo
            {
                TypedLociCount = verificationRun.SearchLociCount,
                Patients = await GetSimulantsInfo(verificationRun.TestHarness_Id, TestIndividualCategory.Patient),
                Donors = await GetSimulantsInfo(verificationRun.TestHarness_Id, TestIndividualCategory.Donor)
            };
            CachedGenotypeSimulantsInfo[verificationRunId] = info;

            return info;
        }

        public async Task<IReadOnlyCollection<PatientDonorPair>> GetOrAddAllPossibleGenotypePatientDonorPairs(int verificationRunId)
        {
            if (CachedPossiblePdps.ContainsKey(verificationRunId))
            {
                return CachedPossiblePdps[verificationRunId];
            }
            
            var info = await GetOrAddGenotypeSimulantsInfo(verificationRunId);

            var patientIds = info.Patients.Ids;
            var donorIds = info.Donors.Ids;

            var allPdps = patientIds.Cartesian(
                donorIds, 
                (patientId, donorId) => new PatientDonorPair
                {
                    PatientGenotypeSimulantId = patientId,
                    DonorGenotypeSimulantId = donorId
                })
                .ToList();

            CachedPossiblePdps[verificationRunId] = allPdps;

            return allPdps;
        }

        private async Task<SimulantsInfo> GetSimulantsInfo(int testHarnessId, TestIndividualCategory category)
        {
            var info = (await simulantsRepository.GetGenotypeSimulants(testHarnessId, category.ToString())).ToList();

            return new SimulantsInfo
            {
                Ids = info.Select(s => s.Id).ToList(),
                Hla = info
            };
        }
    }

    internal class GenotypeSimulantsInfo
    {
        public int TypedLociCount { get; set; }
        public SimulantsInfo Patients { get; set; }
        public SimulantsInfo Donors { get; set; }
    }

    internal class SimulantsInfo
    {
        public IReadOnlyCollection<int> Ids { get; set; }
        public IReadOnlyCollection<Simulant> Hla { get; set; }
    }
}
