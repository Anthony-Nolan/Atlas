using System.Linq;
using Nova.SearchAlgorithm.Test.Validation.TestData.Exceptions;
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
                .Where(md => FulfilsDonorTypeCriteria(criteria, md)
                             && FulfilsRegistryCriteria(criteria, md)
                             && FulfilsTgsTypingCategoryCriteria(criteria, md)
                             && FulfilsMatchLevelCriteria(criteria, md))
                .ToList();

            if (!matchingMetaDonors.Any())
            {
                throw new MetaDonorNotFoundException("No meta-donors found matching specified criteria.");
            }

            return matchingMetaDonors.First();
        }

        private static bool FulfilsMatchLevelCriteria(MetaDonorSelectionCriteria criteria, MetaDonor md)
        {
            return criteria.MatchLevels.ToEnumerable().All(ml => ml != MatchLevel.PGroup)
                   || md.GenotypeCriteria.HasNonUniquePGroups.ToEnumerable().Any(x => x);
        }

        private static bool FulfilsTgsTypingCategoryCriteria(MetaDonorSelectionCriteria criteria, MetaDonor md)
        {
            return criteria.MatchingTgsTypingCategories.Equals(md.GenotypeCriteria.TgsHlaCategories);
        }

        private static bool FulfilsRegistryCriteria(MetaDonorSelectionCriteria criteria, MetaDonor md)
        {
            return criteria.MatchingRegistry == md.Registry;
        }

        private static bool FulfilsDonorTypeCriteria(MetaDonorSelectionCriteria criteria, MetaDonor metaDonor)
        {
            return criteria.MatchingDonorType == metaDonor.DonorType;
        }
    }
}