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
                response.Result!.MatchedGenotypePairs, patientSummaryId, donorSummaryId);
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
            IEnumerable<string> matchingGenotypePairs,
            int patientSummaryId,
            int donorSummaryId)
        {
            // first line contains header so will be skipped
            return matchingGenotypePairs
                .Skip(1)
                .Select(pair => pair.Split(","))
                .Select(pairParts => new MatchingGenotypes
            {
                TotalCount = int.Parse(pairParts[0]),
                A_Count = int.Parse(pairParts[1]),
                B_Count = int.Parse(pairParts[2]),
                C_Count = int.Parse(pairParts[3]),
                DQB1_Count = int.Parse(pairParts[4]),
                DRB1_Count = int.Parse(pairParts[5]),

                Patient_A_1 = pairParts[6],
                Patient_A_2 = pairParts[7],
                Patient_B_1 = pairParts[8],
                Patient_B_2 = pairParts[9],
                Patient_C_1 = pairParts[10],
                Patient_C_2 = pairParts[11],
                Patient_DQB1_1 = pairParts[12],
                Patient_DQB1_2 = pairParts[13],
                Patient_DRB1_1 = pairParts[14],
                Patient_DRB1_2 = pairParts[15],
                Patient_Likelihood = decimal.Parse(pairParts[16]),

                Donor_A_1 = pairParts[17],
                Donor_A_2 = pairParts[18],
                Donor_B_1 = pairParts[19],
                Donor_B_2 = pairParts[20],
                Donor_C_1 = pairParts[21],
                Donor_C_2 = pairParts[22],
                Donor_DQB1_1 = pairParts[23],
                Donor_DQB1_2 = pairParts[24],
                Donor_DRB1_1 = pairParts[25],
                Donor_DRB1_2 = pairParts[26],
                Donor_Likelihood = decimal.Parse(pairParts[27]),

                Patient_ImputationSummary_Id = patientSummaryId,
                Donor_ImputationSummary_Id = donorSummaryId
            });
        }
    }
}
