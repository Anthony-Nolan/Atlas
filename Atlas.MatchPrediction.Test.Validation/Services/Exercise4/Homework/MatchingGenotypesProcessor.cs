using Atlas.MatchPrediction.Test.Validation.Data.Models;
using Atlas.MatchPrediction.Test.Validation.Data.Models.Homework;
using Atlas.MatchPrediction.Test.Validation.Data.Repositories.Homework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Debug.Client.Models.MatchPrediction;
using SubjectInfo = Atlas.MatchPrediction.Test.Validation.Data.Models.SubjectInfo;

namespace Atlas.MatchPrediction.Test.Validation.Services.Exercise4.Homework
{
    internal interface IMatchingGenotypesProcessor
    {
        /// <returns>`false` if request failed; else `true`.</returns>
        Task<bool> RequestAndSaveMatchingGenotypes(
            SubjectInfo patientInfo,
            SubjectInfo donorInfo,
            string matchLoci,
            string hlaVersion);
    }

    internal class MatchingGenotypesProcessor : IMatchingGenotypesProcessor
    {
        private readonly IMatchingGenotypesRequester matchingRequester;
        private readonly IImputationSummaryRepository summaryRepo;
        private readonly IMatchingGenotypesRepository matchingRepo;

        public MatchingGenotypesProcessor(
            IMatchingGenotypesRequester matchingRequester,
            IImputationSummaryRepository summaryRepo,
            IMatchingGenotypesRepository matchingRepo)
        {
            this.matchingRequester = matchingRequester;
            this.summaryRepo = summaryRepo;
            this.matchingRepo = matchingRepo;
        }

        public async Task<bool> RequestAndSaveMatchingGenotypes(
            SubjectInfo patientInfo,
            SubjectInfo donorInfo,
            string matchLoci,
            string hlaVersion)
        {
            var response = await matchingRequester.Request(
                BuildRequest(patientInfo, donorInfo, matchLoci, hlaVersion));

            if (response is not { WasSuccess: true })
            {
                System.Diagnostics.Debug.WriteLine($"Matching genotypes request for {patientInfo.ExternalId}:{donorInfo.ExternalId} was not successful.");
                return false;
            }

            var patientSummaryId = await GetOrAddImputationSummary(patientInfo.ExternalId, response.Result!.PatientInfo);
            var donorSummaryId = await GetOrAddImputationSummary(donorInfo.ExternalId, response.Result!.DonorInfo);
            var genotypes = SplitIntoMatchingGenotypes(
                response.Result!.MatchedGenotypePairs, patientSummaryId, donorSummaryId).ToList();
            await matchingRepo.BulkInsert(genotypes);
            return true;
        }

        private static MatchingGenotypesRequest BuildRequest(
            SubjectInfo patientInfo,
            SubjectInfo donorInfo,
            string matchLoci,
            string hlaVersion)
        {
            return new MatchingGenotypesRequest
            {
                Patient = new MatchingGenotypesRequest.SubjectRequest
                {
                    SubjectHla = patientInfo.ToPhenotypeInfo(),
                    ExternalHfSetId = patientInfo.ExternalHfSetId ?? 0,
                },
                Donor = new MatchingGenotypesRequest.SubjectRequest
                {
                    SubjectHla = donorInfo.ToPhenotypeInfo(),
                    ExternalHfSetId = donorInfo.ExternalHfSetId ?? 0,
                },
                MatchLoci = matchLoci,
                HlaVersion = hlaVersion
            };
        }

        private async Task<int> GetOrAddImputationSummary(
            string externalSubjectId,
            SubjectResult subjectResult)
        {
            var summaryId = await summaryRepo.Get(externalSubjectId);

            if (summaryId != null)
            {
                return summaryId.Value;
            }

            var summary = new ImputationSummary
            {
                ExternalSubjectId = externalSubjectId,
                HfSetPopulationId = subjectResult.HaplotypeFrequencySet.PopulationId,
                WasRepresented = !subjectResult.IsUnrepresented,
                GenotypeCount = subjectResult.GenotypeCount,
                SumOfLikelihoods = subjectResult.SumOfLikelihoods
            };

            return await summaryRepo.Add(summary);
        }

        private static IEnumerable<MatchingGenotypes> SplitIntoMatchingGenotypes(
            string matchingGenotypesAsString,
            int patientSummaryId,
            int donorSummaryId)
        {
            // split genotypesAsString into individual genotypes by splitting by \r\n
            // first line contains header so will be skipped
            var matchingGenotypeStrings = matchingGenotypesAsString
                .Split("\r\n")
                .Skip(1)
                .Where(s => !string.IsNullOrEmpty(s));

            foreach (var matchingGenotypes in matchingGenotypeStrings)
            {
                var matchingGenotypeParts = matchingGenotypes.Split(",");
                yield return new MatchingGenotypes
                {
                    TotalCount = int.Parse(matchingGenotypeParts[0]),
                    A_Count = int.Parse(matchingGenotypeParts[1]),
                    B_Count = int.Parse(matchingGenotypeParts[2]),
                    C_Count = int.Parse(matchingGenotypeParts[3]),
                    DQB1_Count = int.Parse(matchingGenotypeParts[4]),
                    DRB1_Count = int.Parse(matchingGenotypeParts[5]),

                    Patient_A_1 = matchingGenotypeParts[6],
                    Patient_A_2 = matchingGenotypeParts[7],
                    Patient_B_1 = matchingGenotypeParts[8],
                    Patient_B_2 = matchingGenotypeParts[9],
                    Patient_C_1 = matchingGenotypeParts[10],
                    Patient_C_2 = matchingGenotypeParts[11],
                    Patient_DQB1_1 = matchingGenotypeParts[12],
                    Patient_DQB1_2 = matchingGenotypeParts[13],
                    Patient_DRB1_1 = matchingGenotypeParts[14],
                    Patient_DRB1_2 = matchingGenotypeParts[15],
                    Patient_Likelihood = decimal.Parse(matchingGenotypeParts[16]),

                    Donor_A_1 = matchingGenotypeParts[17],
                    Donor_A_2 = matchingGenotypeParts[18],
                    Donor_B_1 = matchingGenotypeParts[19],
                    Donor_B_2 = matchingGenotypeParts[20],
                    Donor_C_1 = matchingGenotypeParts[21],
                    Donor_C_2 = matchingGenotypeParts[22],
                    Donor_DQB1_1 = matchingGenotypeParts[23],
                    Donor_DQB1_2 = matchingGenotypeParts[24],
                    Donor_DRB1_1 = matchingGenotypeParts[25],
                    Donor_DRB1_2 = matchingGenotypeParts[26],
                    Donor_Likelihood = decimal.Parse(matchingGenotypeParts[27]),

                    Patient_ImputationSummary_Id = patientSummaryId,
                    Donor_ImputationSummary_Id = donorSummaryId
                };
            }
        }
    }
}
