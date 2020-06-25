using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.MatchPrediction.Client.Models.MatchProbability;
using Atlas.MatchPrediction.Services.ExpandAmbiguousPhenotype;
using MoreLinq;

namespace Atlas.MatchPrediction.Services.MatchProbability
{
    public interface IMatchProbabilityService
    {
        public Task<MatchProbabilityResponse> CalculateMatchProbability(MatchProbabilityInput matchProbabilityInput);
    }

    public class MatchProbabilityService : IMatchProbabilityService
    {
        private readonly ICompressedPhenotypeExpander compressedPhenotypeExpander;
        private readonly IGenotypeMatcher genotypeMatcher;

        public MatchProbabilityService(
            ICompressedPhenotypeExpander compressedPhenotypeExpander,
            IGenotypeMatcher genotypeMatcher)
        {
            this.compressedPhenotypeExpander = compressedPhenotypeExpander;
            this.genotypeMatcher = genotypeMatcher;
        }

        public async Task<MatchProbabilityResponse> CalculateMatchProbability(MatchProbabilityInput matchProbabilityInput)
        {
            var expandedPatientHla = 
                await compressedPhenotypeExpander.ExpandCompressedPhenotype(matchProbabilityInput.PatientHla, matchProbabilityInput.HlaNomenclatureVersion);
            var patientGenotypes = expandedPatientHla.ToHashSet();
            var expandedDonorHla =
                await compressedPhenotypeExpander.ExpandCompressedPhenotype(matchProbabilityInput.DonorHla, matchProbabilityInput.HlaNomenclatureVersion);
            var donorGenotypes = expandedDonorHla.ToHashSet();


            var matchingPairs = 
                await genotypeMatcher.PairsWithTenOutOfTenMatch(patientGenotypes, donorGenotypes, matchProbabilityInput.HlaNomenclatureVersion);

            return new MatchProbabilityResponse();
        }
    }
}