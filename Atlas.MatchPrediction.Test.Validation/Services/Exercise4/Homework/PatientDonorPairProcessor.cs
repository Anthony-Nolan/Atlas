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
        private readonly IMatchingGenotypesProcessor processor;

        public PatientDonorPairProcessor(
            IPatientDonorPairRepository pdpRepository,
            IMissingHlaChecker missingHlaChecker,
            IMatchingGenotypesProcessor processor)
        {
            this.pdpRepository = pdpRepository;
            this.missingHlaChecker = missingHlaChecker;
            this.processor = processor;
        }

        /// <inheritdoc />
        public async Task Process(PatientDonorPair pdp, string matchLoci, string hlaVersion)
        {
            var matchLociInfo = matchLoci.ToLociInfo();

            var patientResult = await CheckPatientHasMissingHla(pdp, matchLociInfo);
            if (patientResult.HasMissingHla) return;

            var donorResult = await CheckDonorHasMissingHla(pdp, matchLociInfo);
            if (donorResult.HasMissingHla) return;

            await MatchGenotypes(patientResult, donorResult, matchLoci, hlaVersion, pdp);
        }

        private async Task<SubjectGenotypeResult> CheckPatientHasMissingHla(PatientDonorPair pdp, LociInfo<bool> matchLoci)
        {
            async Task<SubjectGenotypeResult> GetResult()
            {
                var (hasMissingHla, patientInfo) = await missingHlaChecker.SubjectHasMissingHla(pdp.PatientId, false, matchLoci);
                pdp.DidPatientHaveMissingHla = hasMissingHla;
                pdp.IsProcessed = hasMissingHla;
                await UpdatePatientDonorPair(pdp);
                return new SubjectGenotypeResult(hasMissingHla, patientInfo);
            }

            return subjectResultCache.GetOrAdd(pdp.PatientId, await GetResult());
        }

        private async Task<SubjectGenotypeResult> CheckDonorHasMissingHla(PatientDonorPair pdp, LociInfo<bool> matchLoci)
        {
            async Task<SubjectGenotypeResult> GetResult()
            {
                var (hasMissingHla, donorInfo) = await missingHlaChecker.SubjectHasMissingHla(pdp.DonorId, true, matchLoci);
                pdp.DidDonorHaveMissingHla = hasMissingHla;
                pdp.IsProcessed = hasMissingHla;
                await UpdatePatientDonorPair(pdp);
                return new SubjectGenotypeResult(hasMissingHla, donorInfo);
            }

            return subjectResultCache.GetOrAdd(pdp.DonorId, await GetResult());
        }

        private async Task MatchGenotypes(
            SubjectGenotypeResult patientResult,
            SubjectGenotypeResult donorResult,
            string matchLoci,
            string hlaVersion,
            PatientDonorPair pdp)
        {
            var matchingResult = await processor.RequestAndSaveMatchingGenotypes(
                patientResult.SubjectInfo, donorResult.SubjectInfo, matchLoci, hlaVersion);

            if (matchingResult)
            {
                pdp.MatchingGenotypesCalculated = true;
                pdp.IsProcessed = true;
            }

            await UpdatePatientDonorPair(pdp);
        }

        private async Task UpdatePatientDonorPair(PatientDonorPair pdp)
        {
            await pdpRepository.UpdateEditableFields(pdp);
        }
    }
}