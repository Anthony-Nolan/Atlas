using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Utils.Extensions;
using Atlas.MatchPrediction.Test.Verification.Data.Models;
using Atlas.MatchPrediction.Test.Verification.Data.Models.Entities.TestHarness;
using Atlas.MatchPrediction.Test.Verification.Data.Models.Entities.Verification;
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
        private static readonly IDictionary<int, VerificationRun> CachedVerificationRuns = 
            new ConcurrentDictionary<int, VerificationRun>();
        private static readonly IDictionary<int, GenotypeSimulantsInfo> CachedGenotypeSimulantsInfo =
            new ConcurrentDictionary<int, GenotypeSimulantsInfo>();
        private static readonly IDictionary<int, IReadOnlyCollection<PatientDonorPair>> CachedPossiblePdps =
            new ConcurrentDictionary<int, IReadOnlyCollection<PatientDonorPair>>();

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
            var testHarnessId = await GetTestHarnessId(verificationRunId);

            return await CachedGenotypeSimulantsInfo.GetOrAddAsync(testHarnessId, async () =>
                new GenotypeSimulantsInfo
                {
                    TypedLociCount = await GetSearchLociCount(verificationRunId),
                    Patients = await GetSimulantsInfo(testHarnessId, TestIndividualCategory.Patient),
                    Donors = await GetSimulantsInfo(testHarnessId, TestIndividualCategory.Donor)
                });
        }

        public async Task<IReadOnlyCollection<PatientDonorPair>> GetOrAddAllPossibleGenotypePatientDonorPairs(int verificationRunId)
        {
            var testHarnessId = await GetTestHarnessId(verificationRunId);

            return await CachedPossiblePdps.GetOrAddAsync<int, IReadOnlyCollection<PatientDonorPair>>(
                testHarnessId,
                async () =>
                {
                    var info = await GetOrAddGenotypeSimulantsInfo(verificationRunId);
                    return info.Patients.Ids.Cartesian(
                            info.Donors.Ids,
                            (patientId, donorId) => new PatientDonorPair
                            {
                                PatientGenotypeSimulantId = patientId,
                                DonorGenotypeSimulantId = donorId
                            })
                        .ToList();
                });
        }

        private async Task<int> GetTestHarnessId(int verificationRunId)
        {
            return (await GetVerificationRun(verificationRunId)).TestHarness_Id;
        }

        private async Task<int> GetSearchLociCount(int verificationRunId)
        {
            return (await GetVerificationRun(verificationRunId)).SearchLociCount;
        }

        private async Task<VerificationRun> GetVerificationRun(int verificationRunId)
        {
            return await CachedVerificationRuns.GetOrAddAsync(
                verificationRunId,
                async () => await verificationRunRepository.GetVerificationRun(verificationRunId));
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