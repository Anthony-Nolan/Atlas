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
                             && FulfilsMatchLevelCriteria(criteria, md)
                             && FulfilsTypingResolutionCriteria(criteria, md))
                .ToList();

            if (!matchingMetaDonors.Any())
            {
                throw new MetaDonorNotFoundException("No meta-donors found matching specified criteria.");
            }

            return matchingMetaDonors.First();
        }

        private static bool FulfilsTypingResolutionCriteria(MetaDonorSelectionCriteria criteria, MetaDonor metaDonor)
        {
            return metaDonor.HlaTypingResolutionSets.Any(resolutions => criteria.TypingResolutions.Equals(resolutions));
        }

        private static bool FulfilsMatchLevelCriteria(MetaDonorSelectionCriteria criteria, MetaDonor metaDonor)
        {
            // TODO: NOVA-1662: Allow p/g-group matching per-locus
            if (criteria.MatchLevels.ToEnumerable().All(ml => ml == MatchLevel.PGroup))
            {
                return metaDonor.GenotypeCriteria.PGroupMatchPossible.ToEnumerable().All(x => x);
            }
            if (criteria.MatchLevels.ToEnumerable().All(ml => ml == MatchLevel.GGroup))
            {
                return metaDonor.GenotypeCriteria.GGroupMatchPossible.ToEnumerable().All(x => x);
            }

            return true;
        }

        private static bool FulfilsTgsTypingCategoryCriteria(MetaDonorSelectionCriteria criteria, MetaDonor metaDonor)
        {
            return criteria.MatchingTgsTypingCategories.Equals(metaDonor.GenotypeCriteria.TgsHlaCategories);
        }

        private static bool FulfilsRegistryCriteria(MetaDonorSelectionCriteria criteria, MetaDonor metaDonor)
        {
            return criteria.MatchingRegistry == metaDonor.Registry;
        }

        private static bool FulfilsDonorTypeCriteria(MetaDonorSelectionCriteria criteria, MetaDonor metaDonor)
        {
            return criteria.MatchingDonorType == metaDonor.DonorType;
        }
    }
}