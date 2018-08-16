using System.Linq;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models.PatientDataSelection;
using Nova.SearchAlgorithm.Test.Validation.TestData.Repositories;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Services.PatientDataSelection
{
    public interface IMetaDonorSelector
    {
        MetaDonor GetMetaDonor(MetaDonorSelectionCriteria criteria);
    }

    public class MetaDonorSelector : IMetaDonorSelector
    {
        private readonly IMetaDonorRepository metaDonorRepository;

        public MetaDonorSelector(IMetaDonorRepository metaDonorRepository)
        {
            this.metaDonorRepository = metaDonorRepository;
        }

        public MetaDonor GetMetaDonor(MetaDonorSelectionCriteria criteria)
        {
            var matchingMetaDonors = metaDonorRepository.AllMetaDonors()
                .Where(md => criteria.MatchingDonorType == md.DonorType)
                .Where(md => criteria.MatchingRegistry == md.Registry)
                .Where(md => criteria.MatchingTgsTypingCategories.Equals(md.GenotypeCriteria.TgsHlaCategories))
                .Where(md => criteria.MatchLevels.ToEnumerable().All(ml => ml != MatchLevel.PGroup)
                             || md.GenotypeCriteria.HasNonUniquePGroups.ToEnumerable().Any(x => x));
            return matchingMetaDonors.First();
        }
    }
}