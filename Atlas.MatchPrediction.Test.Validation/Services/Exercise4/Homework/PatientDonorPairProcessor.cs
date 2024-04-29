using System.Collections.Concurrent;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.MatchPrediction.Test.Validation.Data.Models.Homework;
using Atlas.MatchPrediction.Test.Validation.Data.Repositories.Homework;
using Atlas.MatchPrediction.Test.Validation.Models;

// ReSharper disable InconsistentNaming

namespace Atlas.MatchPrediction.Test.Validation.Services.Exercise4.Homework
{
    public interface IPatientDonorPairProcessor
    {
        Task Process(PatientDonorPair pdp, string matchLoci, string hlaVersion);
    }

    internal class PatientDonorPairProcessor : IPatientDonorPairProcessor
    {
        private static readonly ConcurrentDictionary<string, SubjectGenotypeResult> subjectResultCache = new();

        private readonly IPatientDonorPairRepository pdpRepository;
        private readonly IMissingHlaChecker missingHlaChecker;
        private readonly ISubjectGenotypesProcessor genotypesProcessor;

        public PatientDonorPairProcessor(
            IPatientDonorPairRepository pdpRepository,
            IMissingHlaChecker missingHlaChecker,
            ISubjectGenotypesProcessor genotypesProcessor)
        {
            this.pdpRepository = pdpRepository;
            this.missingHlaChecker = missingHlaChecker;
            this.genotypesProcessor = genotypesProcessor;
        }

        /// <inheritdoc />
        public async Task Process(PatientDonorPair pdp, string matchLoci, string hlaVersion)
        {
            var matchLociInfo = matchLoci.ToLociInfo();

            var patientResult = await CheckPatientHasMissingHla(pdp, matchLociInfo);
            if (patientResult.HasMissingHla) return;

            var donorResult = await CheckDonorHasMissingHla(pdp, matchLociInfo);
            if (donorResult.HasMissingHla) return;

            await Task.WhenAll(
                Impute(pdp, patientResult, matchLoci, hlaVersion, true),
                Impute(pdp, donorResult, matchLoci, hlaVersion, false)
            // Then submit matching genotypes request
            );
        }

        private async Task<SubjectGenotypeResult> CheckPatientHasMissingHla(PatientDonorPair pdp, LociInfo<bool> matchLoci)
        {
            async Task<SubjectGenotypeResult> GetResult()
            {
                var (hasMissingHla, patientInfo) = await missingHlaChecker.SubjectHasMissingHla(pdp.PatientId, false, matchLoci);
                pdp.DidPatientHaveMissingHla = hasMissingHla;
                pdp.IsProcessed = hasMissingHla;
                await UpdateRecord(pdp);
                return new SubjectGenotypeResult(hasMissingHla, patientInfo);
            }

            return subjectResultCache.GetOrAdd(pdp.PatientId, await GetResult());
        }

        private async Task<SubjectGenotypeResult> CheckDonorHasMissingHla(PatientDonorPair pdp, LociInfo<bool> matchLoci)
        {
            async Task<SubjectGenotypeResult> GetResult()
            {
                var (hasMissingHla, donorInfo) = await missingHlaChecker.SubjectHasMissingHla(pdp.DonorId, false, matchLoci);
                pdp.DidDonorHaveMissingHla = hasMissingHla;
                pdp.IsProcessed = hasMissingHla;
                await UpdateRecord(pdp);
                return new SubjectGenotypeResult(hasMissingHla, donorInfo);
            }

            return subjectResultCache.GetOrAdd(pdp.DonorId, await GetResult());
        }

        private async Task Impute(
            PatientDonorPair pdp,
            SubjectGenotypeResult result,
            string matchLoci,
            string hlaVersion,
            bool isPatient)
        {
            // Only request imputation if subject has not been processed before
            // This step should update the cache as well
            result.Genotypes ??= await genotypesProcessor.RequestAndSaveImputation(result.SubjectInfo, matchLoci, hlaVersion);

            if (isPatient)
            {
                pdp.PatientImputationCompleted = true;
            }
            else
            {
                pdp.DonorImputationCompleted = true;
            }
            
            await UpdateRecord(pdp);
        }

        private async Task UpdateRecord(PatientDonorPair pdp)
        {
            await pdpRepository.UpdateEditableFields(pdp);
        }
    }
}