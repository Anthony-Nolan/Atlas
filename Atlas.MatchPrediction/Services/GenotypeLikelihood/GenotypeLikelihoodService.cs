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
        public GenotypeLikelihoodResponse CalculateLikelihood(GenotypeLikelihoodInput genotypeLikelihood);
    }

    public class GenotypeLikelihoodService : IGenotypeLikelihoodService
    {
        private readonly IGenotypeImputer genotypeImputer;

        public GenotypeLikelihoodService(IGenotypeImputer genotypeImputer)
        {
            this.genotypeImputer = genotypeImputer;
        }

        public GenotypeLikelihoodResponse CalculateLikelihood(GenotypeLikelihoodInput genotypeLikelihood)
        {
            var diplotypes = genotypeImputer.GetPossibleDiplotypes(genotypeLikelihood.Genotype);
            var haplotypes = GetHaplotypes(diplotypes).ToList();
            var haplotypesWithFrequencies = await haplotypeFrequencies.GetDiplotypeFrequencies(haplotypes);

            GetFrequenciesForDiplotype(haplotypesWithFrequencies, diplotypes);

            return new GenotypeLikelihoodResponse() {Likelihood = 1};
        }

        private static IEnumerable<LociInfo<string>> GetHaplotypes(IEnumerable<Diplotype> diplotypes)
        {
            return diplotypes.SelectMany(diplotype => new List<LociInfo<string>>
                {diplotype.Item1.Hla, diplotype.Item2.Hla});
        }

        private static void GetFrequenciesForDiplotype(
            Dictionary<LociInfo<string>, decimal> haplotypesWithFrequencies, IEnumerable<Diplotype> diplotypes)
        {
            foreach (var diplotype in diplotypes)
            {
                diplotype.Item1.Frequency = haplotypesWithFrequencies[diplotype.Item1.Hla];
                diplotype.Item2.Frequency = haplotypesWithFrequencies[diplotype.Item2.Hla];
            }
        }
    }
}
