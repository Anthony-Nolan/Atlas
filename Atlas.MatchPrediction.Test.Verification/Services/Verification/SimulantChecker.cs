using System.Linq;
using System.Threading.Tasks;

namespace Atlas.MatchPrediction.Test.Verification.Services.Verification
{
    internal interface ISimulantChecker
    {
        Task<bool> IsPatientAGenotypeSimulant(int verificationRunId, int patientSimulantId);
    }

    internal class SimulantChecker : ISimulantChecker
    {
        private readonly IGenotypeSimulantsInfoCache cache;

        public SimulantChecker(IGenotypeSimulantsInfoCache cache)
        {
            this.cache = cache;
        }

        public async Task<bool> IsPatientAGenotypeSimulant(int verificationRunId, int patientSimulantId)
        {
            var info = await cache.GetOrAddGenotypeSimulantsInfo(verificationRunId);

            return info.Patients.Ids.Contains(patientSimulantId);
        }
    }
}
