using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.MatchPrediction.Client.Models.GenotypeLikelihood;
using Atlas.MatchPrediction.Data.Repositories;
using Atlas.MatchPrediction.Models;

namespace Atlas.MatchPrediction.Services.GenotypeLikelihood
{
    public interface IGenotypeLikelihoodService
    {
        public Task<GenotypeLikelihoodResponse> CalculateLikelihood(GenotypeLikelihoodInput genotypeLikelihood);
    }

    public class GenotypeLikelihoodService : IGenotypeLikelihoodService
    {
        private readonly IHaplotypeFrequenciesRepository haplotypeFrequenciesRepository;
        private readonly IGenotypeImputer genotypeImputer;

        public GenotypeLikelihoodService(IHaplotypeFrequenciesRepository haplotypeFrequenciesRepository,
            IGenotypeImputer genotypeImputer)
        {
            this.haplotypeFrequenciesRepository = haplotypeFrequenciesRepository;
            this.genotypeImputer = genotypeImputer;
        }

        public async Task<GenotypeLikelihoodResponse> CalculateLikelihood(GenotypeLikelihoodInput genotypeLikelihood)
        {
            var diplotypes = genotypeImputer.GetPossibleDiplotypes(genotypeLikelihood.Genotype);
            var haplotypes = GetHaplotypes(diplotypes).ToList();
            var haplotypesWithFrequencies = await haplotypeFrequenciesRepository.GetDiplotypeFrequencies(haplotypes);

            return new GenotypeLikelihoodResponse() {Likelihood = 1};
        }

        private static IEnumerable<LociInfo<string>> GetHaplotypes(IEnumerable<Diplotype> diplotypes)
        {
            return diplotypes.SelectMany(diplotype => new List<LociInfo<string>>
                {diplotype.Item1.Hla, diplotype.Item2.Hla});
        }
    }
}