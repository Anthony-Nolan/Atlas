using Atlas.Debug.Client.Models.MatchPrediction;
using Atlas.MatchPrediction.Test.Validation.Data.Models;
using Atlas.MatchPrediction.Test.Validation.Data.Models.Homework;
using Atlas.MatchPrediction.Test.Validation.Data.Repositories.Homework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SubjectInfo = Atlas.MatchPrediction.Test.Validation.Data.Models.SubjectInfo;

namespace Atlas.MatchPrediction.Test.Validation.Services.Exercise4.Homework
{
    internal interface ISubjectGenotypesProcessor
    {
        /// <returns>`null` means the imputation request failed.
        /// Empty collection means success, but no genotypes found, which is a valid response.</returns>
        Task<IEnumerable<SubjectGenotype>> RequestAndSaveImputation(
            SubjectInfo subjectInfo,
            string matchLoci,
            string hlaVersion);
    }

    internal class SubjectGenotypesProcessor : ISubjectGenotypesProcessor
    {
        private readonly IImputationRequester imputationRequester;
        private readonly IImputationSummaryRepository summaryRepo;
        private readonly ISubjectGenotypeRepository subjectGenotypeRepo;

        public SubjectGenotypesProcessor(
            IImputationRequester imputationRequester, 
            IImputationSummaryRepository summaryRepo,
            ISubjectGenotypeRepository subjectGenotypeRepo)
        {
            this.imputationRequester = imputationRequester;
            this.summaryRepo = summaryRepo;
            this.subjectGenotypeRepo = subjectGenotypeRepo;
        }

        public async Task<IEnumerable<SubjectGenotype>> RequestAndSaveImputation(
            SubjectInfo subjectInfo,
            string matchLoci,
            string hlaVersion)
        {
            var subjectId = subjectInfo.ExternalId;
            var summaryId = await summaryRepo.Get(subjectId);

            if (summaryId != null)
            {
                return await subjectGenotypeRepo.Get(summaryId.Value);
            }

            var response = await imputationRequester.Request(new HomeworkImputationRequest
            {
                SubjectHla = subjectInfo.ToPhenotypeInfo(),
                ExternalHfSetId = subjectInfo.ExternalHfSetId,
                MatchLoci = matchLoci,
                HlaVersion = hlaVersion
            });

            // if response is not successful, return, the PDP will remain as "unprocessed" in the db
            if (response is not { WasSuccess: true })
            {
                System.Diagnostics.Debug.WriteLine($"Imputation request for {subjectId} was not successful.");
                return null;
            }

            summaryId = await summaryRepo.Add(BuildSummary(subjectId, response.Result!));
            var genotypes = SplitIntoGenotypes(response.Result!.GenotypeLikelihoods, summaryId.Value).ToList();
            await subjectGenotypeRepo.BulkInsert(genotypes);
            return genotypes;
        }

        private static ImputationSummary BuildSummary(
            string externalSubjectId,
            GenotypeImputationResponse genotypeResponse)
        {
            return new ImputationSummary
            {
                ExternalSubjectId = externalSubjectId,
                HfSetPopulationId = genotypeResponse.HaplotypeFrequencySet.PopulationId,
                WasRepresented = !genotypeResponse.IsUnrepresented,
                GenotypeCount = genotypeResponse.GenotypeCount,
                SumOfLikelihoods = genotypeResponse.SumOfLikelihoods
            };
        }

        private static IEnumerable<SubjectGenotype> SplitIntoGenotypes(
            string genotypesAsString, int imputationSummaryId)
        {
            // split genotypesAsString into individual genotypes by splitting by \r\n
            // first line contains header so will be skipped
            var genotypeStrings = genotypesAsString
                .Split("\r\n")
                .Skip(1)
                .Where(s => !string.IsNullOrEmpty(s));

            foreach (var genotypeString in genotypeStrings)
            {
                var genotypeParts = genotypeString.Split(",");
                yield return new SubjectGenotype
                {
                    A_1 = genotypeParts[0],
                    A_2 = genotypeParts[1],
                    B_1 = genotypeParts[2],
                    B_2 = genotypeParts[3],
                    C_1 = genotypeParts[4],
                    C_2 = genotypeParts[5],
                    DQB1_1 = genotypeParts[6],
                    DQB1_2 = genotypeParts[7],
                    DRB1_1 = genotypeParts[8],
                    DRB1_2 = genotypeParts[9],
                    Likelihood = decimal.Parse(genotypeParts[10]),
                    ImputationSummary_Id = imputationSummaryId
                };
            }
        }
    }
}
