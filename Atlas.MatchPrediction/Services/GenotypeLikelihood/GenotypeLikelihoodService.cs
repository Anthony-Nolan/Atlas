using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.MatchPrediction.Data.Repositories;
using Atlas.MatchPrediction.ExternalInterface.Models;
using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using HaplotypeHla = Atlas.Common.GeneticData.PhenotypeInfo.LociInfo<string>;

namespace Atlas.MatchPrediction.Services.GenotypeLikelihood
{
    public interface IGenotypeLikelihoodService
    {
        public Task<decimal> CalculateLikelihood(PhenotypeInfo<string> genotype,
            FrequencySetMetadata donorFrequencySetMetadata,
            FrequencySetMetadata patientFrequencySetMetadata,
            bool isPatient);
    }

    internal class GenotypeLikelihoodService : IGenotypeLikelihoodService
    {
        private readonly IHaplotypeFrequencySetRepository setRepository;
        private readonly IHaplotypeFrequenciesRepository frequencyRepository;
        private readonly IUnambiguousGenotypeExpander unambiguousGenotypeExpander;
        private readonly IGenotypeLikelihoodCalculator likelihoodCalculator;
        private readonly IFrequencySetService frequencySetService;

        public GenotypeLikelihoodService(
            IHaplotypeFrequencySetRepository setRepository,
            IHaplotypeFrequenciesRepository frequencyRepository,
            IUnambiguousGenotypeExpander unambiguousGenotypeExpander,
            IGenotypeLikelihoodCalculator likelihoodCalculator,
            IFrequencySetService frequencySetService)
        {
            this.setRepository = setRepository;
            this.frequencyRepository = frequencyRepository;
            this.unambiguousGenotypeExpander = unambiguousGenotypeExpander;
            this.likelihoodCalculator = likelihoodCalculator;
            this.frequencySetService = frequencySetService;
        }

        public async Task<decimal> CalculateLikelihood(PhenotypeInfo<string> genotype,
            FrequencySetMetadata donorFrequencySetMetadata,
            FrequencySetMetadata patientFrequencySetMetadata,
            bool isPatient)
        {
            var expandedGenotype = unambiguousGenotypeExpander.ExpandGenotype(genotype);

            var frequencySet =
                await frequencySetService.GetHaplotypeFrequencySets(donorFrequencySetMetadata,
                    patientFrequencySetMetadata);
            var setToUse = isPatient ? frequencySet.PatientSet : frequencySet.DonorSet;
            var haplotypesWithFrequencies =
                await GetHaplotypesWithFrequencies(expandedGenotype, setToUse);

            UpdateFrequenciesForDiplotype(haplotypesWithFrequencies, expandedGenotype.Diplotypes);
            return likelihoodCalculator.CalculateLikelihood(expandedGenotype);
        }

        private async Task<Dictionary<HaplotypeHla, decimal>> GetHaplotypesWithFrequencies(
            ExpandedGenotype expandedGenotype,
            HaplotypeFrequencySet haplotypeFrequencySet
        )
        {
            var haplotypes = GetHaplotypes(expandedGenotype.Diplotypes);


            return await frequencyRepository.GetHaplotypeFrequencies(haplotypes, haplotypeFrequencySet.Id);
        }

        public IEnumerable<HaplotypeHla> GetHaplotypes(IEnumerable<Diplotype> diplotypes)
        {
            return diplotypes.SelectMany(diplotype => new List<HaplotypeHla>
                {diplotype.Item1.Hla, diplotype.Item2.Hla});
        }

        private static void UpdateFrequenciesForDiplotype(
            Dictionary<HaplotypeHla, decimal> haplotypesWithFrequencies,
            IEnumerable<Diplotype> diplotypes)
        {
            foreach (var diplotype in diplotypes)
            {
                diplotype.Item1.Frequency = haplotypesWithFrequencies[diplotype.Item1.Hla];
                diplotype.Item2.Frequency = haplotypesWithFrequencies[diplotype.Item2.Hla];
            }
        }
    }
}