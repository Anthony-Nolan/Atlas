using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models.Hla;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models.PatientDataSelection;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Services.PatientDataSelection
{
    /// <summary>
    /// Stores various search criteria from the feature file, and selects appropriate patient data.
    /// The criteria will map to multiple expected donors from the database
    /// e.g. A 10/10 adult match with multiple typing resolutions
    /// </summary>
    public interface IMultipleDonorPatientDataSelector
    {
        void AddFullDonorTypingResolution(PhenotypeInfo<HlaTypingResolution> resolutions);

        IEnumerable<int> GetExpectedMatchingDonorIds();
    }

    public class MultipleDonorPatientDataSelector : IMultipleDonorPatientDataSelector, IPatientHlaContainer
    {
        private readonly IMetaDonorSelector metaDonorSelector;
        private readonly IDatabaseDonorSelector databaseDonorSelector;
        private readonly IPatientHlaSelector patientHlaSelector;

        private MetaDonor selectedMetaDonor;

        private static readonly PhenotypeInfo<MatchLevel> DefaultMatchLevels = new PhenotypeInfo<MatchLevel>(MatchLevel.Allele);

        private static readonly PhenotypeInfo<HlaTypingResolution> DefaultTypingResolutions =
            new PhenotypeInfo<HlaTypingResolution>(HlaTypingResolution.Tgs);

        private readonly MetaDonorSelectionCriteria metaDonorSelectionCriteria = new MetaDonorSelectionCriteria
        {
            MatchLevels = DefaultMatchLevels,
            TypingResolutionSets = new List<PhenotypeInfo<HlaTypingResolution>> {DefaultTypingResolutions},
            MatchingTgsTypingCategories = new PhenotypeInfo<TgsHlaTypingCategory>(TgsHlaTypingCategory.FourFieldAllele),
            // TODO: NOVA-1664: Use feature file to drive this meta-donor level data
            MatchingDonorType = DonorType.Adult,
            MatchingRegistry = RegistryCode.AN,
        };

        private readonly List<DatabaseDonorSelectionCriteria> databaseDonorSelectionCriteriaSet =
            new List<DatabaseDonorSelectionCriteria>
            {
                new DatabaseDonorSelectionCriteria {MatchingTypingResolutions = DefaultTypingResolutions},
            };

        private readonly PatientHlaSelectionCriteria patientHlaSelectionCriteria = new PatientHlaSelectionCriteria {MatchLevels = DefaultMatchLevels};

        public MultipleDonorPatientDataSelector(
            IMetaDonorSelector metaDonorSelector,
            IDatabaseDonorSelector databaseDonorSelector,
            IPatientHlaSelector patientHlaSelector
        )
        {
            this.metaDonorSelector = metaDonorSelector;
            this.databaseDonorSelector = databaseDonorSelector;
            this.patientHlaSelector = patientHlaSelector;
        }

        public void SetAsTenOutOfTenMatch()
        {
            var matches = new PhenotypeInfo<bool>().Map((locus, p, noop) => locus != Locus.Dpb1);
            patientHlaSelectionCriteria.HlaMatches = matches;
        }

        public void SetMatchingDonorType(DonorType donorType)
        {
            metaDonorSelectionCriteria.MatchingDonorType = donorType;
        }

        public void SetMatchingRegistry(RegistryCode registry)
        {
            metaDonorSelectionCriteria.MatchingRegistry = registry;
        }

        /// <summary>
        /// Will set the desired tgs typing category at all positions
        /// </summary>
        public void SetFullMatchingTgsCategory(TgsHlaTypingCategory tgsCategory)
        {
            var categories = new PhenotypeInfo<TgsHlaTypingCategory>(tgsCategory);
            metaDonorSelectionCriteria.MatchingTgsTypingCategories = categories;
        }

        public void AddFullDonorTypingResolution(PhenotypeInfo<HlaTypingResolution> resolutions)
        {
            AddTypingResolutions(resolutions);
        }

        public PhenotypeInfo<string> GetPatientHla()
        {
            return patientHlaSelector.GetPatientHla(GetMetaDonor(), patientHlaSelectionCriteria);
        }

        public IEnumerable<int> GetExpectedMatchingDonorIds()
        {
            return databaseDonorSelectionCriteriaSet.Select(c => databaseDonorSelector.GetExpectedMatchingDonorId(GetMetaDonor(), c));
        }

        private MetaDonor GetMetaDonor()
        {
            // Cache the selected meta-donor to ensure we do not have to perform this calculation multiple times
            if (selectedMetaDonor == null)
            {
                selectedMetaDonor = metaDonorSelector.GetNextMetaDonor(metaDonorSelectionCriteria);
            }

            return selectedMetaDonor;
        }

        /// <summary>
        /// This method should be used to add expected typing resolutions for meta donor and database donor selction criteria,
        /// as both criteria rely on this data
        /// </summary>
        private void AddTypingResolutions(PhenotypeInfo<HlaTypingResolution> resolutions)
        {
            metaDonorSelectionCriteria.TypingResolutionSets.Add(resolutions);
            databaseDonorSelectionCriteriaSet.Add(new DatabaseDonorSelectionCriteria
            {
                MatchingTypingResolutions = resolutions,
            });
        }
    }
}