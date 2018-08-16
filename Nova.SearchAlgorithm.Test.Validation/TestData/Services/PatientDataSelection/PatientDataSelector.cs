using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models.Hla;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models.PatientDataSelection;
using Locus = Nova.SearchAlgorithm.Common.Models.Locus;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Services.PatientDataSelection
{
    public interface IPatientDataSelector
    {
        void SetPatientUntypedAt(Locus locus);
        void SetAsTenOutOfTenMatch();
        void SetAsEightOutOfEightMatch();
        void SetAsSixOutOfSixMatch();
        void SetMatchingDonorUntypedAtLocus(Locus locus);
        void SetFullMatchingTypingResolution(HlaTypingResolution resolution);
        void SetFullMatchingTgsCategory(TgsHlaTypingCategory tgsCategory);
        void SetAsMatchLevelAtAllLoci(MatchLevel matchLevel);
        void SetMatchingRegistry(RegistryCode registry);
        void SetMatchingDonorType(DonorType donorType);

        PhenotypeInfo<string> GetPatientHla();
        int GetExpectedMatchingDonorId();
    }

    /// <summary>
    /// Stores various search criteria from the feature file, and selects appropriate patient data
    /// e.g. A 9/10 adult match with mismatch at A, from AN registry
    /// </summary>
    public class PatientDataSelector : IPatientDataSelector
    {
        public bool HasMatch { get; set; }

        private readonly IMetaDonorSelector metaDonorSelector;
        private readonly IDatabaseDonorSelector databaseDonorSelector;
        private readonly IPatientHlaSelector patientHlaSelector;

        private MetaDonor selectedMetaDonor;

        private static readonly PhenotypeInfo<MatchLevel> DefaultMatchLevels = new PhenotypeInfo<MatchLevel>
        {
            A_1 = MatchLevel.Allele,
            A_2 = MatchLevel.Allele,
            B_1 = MatchLevel.Allele,
            B_2 = MatchLevel.Allele,
            C_1 = MatchLevel.Allele,
            C_2 = MatchLevel.Allele,
            DPB1_1 = MatchLevel.Allele,
            DPB1_2 = MatchLevel.Allele,
            DQB1_1 = MatchLevel.Allele,
            DQB1_2 = MatchLevel.Allele,
            DRB1_1 = MatchLevel.Allele,
            DRB1_2 = MatchLevel.Allele,
        };

        private readonly MetaDonorSelectionCriteria metaDonorSelectionCriteria = new MetaDonorSelectionCriteria {MatchLevels = DefaultMatchLevels};
        private readonly DatabaseDonorSelectionCriteria databaseDonorSelectionCriteria = new DatabaseDonorSelectionCriteria();
        private readonly PatientHlaSelectionCriteria patientHlaSelectionCriteria = new PatientHlaSelectionCriteria {MatchLevels = DefaultMatchLevels};

        public PatientDataSelector(
            IMetaDonorSelector metaDonorSelector,
            IDatabaseDonorSelector databaseDonorSelector,
            IPatientHlaSelector patientHlaSelector
        )
        {
            this.metaDonorSelector = metaDonorSelector;
            this.databaseDonorSelector = databaseDonorSelector;
            this.patientHlaSelector = patientHlaSelector;
        }

        public void SetPatientUntypedAt(Locus locus)
        {
            patientHlaSelectionCriteria.PatientTypingResolutions.SetAtLocus(locus, TypePositions.Both, HlaTypingResolution.Untyped);
        }

        public void SetAsTenOutOfTenMatch()
        {
            patientHlaSelectionCriteria.HlaMatches = patientHlaSelectionCriteria.HlaMatches.Map((locus, p, hasMatch) => 
                locus != Locus.Dpb1);
        }

        public void SetAsEightOutOfEightMatch()
        {
            patientHlaSelectionCriteria.HlaMatches = patientHlaSelectionCriteria.HlaMatches.Map((locus, p, hasMatch) => 
                locus != Locus.Dqb1 || locus != Locus.Dpb1);
        }

        public void SetAsSixOutOfSixMatch()
        {
            patientHlaSelectionCriteria.HlaMatches = patientHlaSelectionCriteria.HlaMatches.Map((locus, p, hasMatch) => 
                locus != Locus.C || locus != Locus.Dqb1 || locus != Locus.Dpb1);
        }

        /// <summary>
        /// Will set the desired typing resolution at all positions
        /// </summary>
        public void SetFullMatchingTypingResolution(HlaTypingResolution resolution)
        {
            databaseDonorSelectionCriteria.MatchingTypingResolutions.A_1 = resolution;
            databaseDonorSelectionCriteria.MatchingTypingResolutions.A_2 = resolution;
            databaseDonorSelectionCriteria.MatchingTypingResolutions.B_1 = resolution;
            databaseDonorSelectionCriteria.MatchingTypingResolutions.B_2 = resolution;
            databaseDonorSelectionCriteria.MatchingTypingResolutions.C_1 = resolution;
            databaseDonorSelectionCriteria.MatchingTypingResolutions.C_2 = resolution;
            databaseDonorSelectionCriteria.MatchingTypingResolutions.DPB1_1 = resolution;
            databaseDonorSelectionCriteria.MatchingTypingResolutions.DPB1_2 = resolution;
            databaseDonorSelectionCriteria.MatchingTypingResolutions.DQB1_1 = resolution;
            databaseDonorSelectionCriteria.MatchingTypingResolutions.DQB1_2 = resolution;
            databaseDonorSelectionCriteria.MatchingTypingResolutions.DRB1_1 = resolution;
            databaseDonorSelectionCriteria.MatchingTypingResolutions.DRB1_2 = resolution;
        }

        /// <summary>
        /// Will set the desired tgs typing category at all positions
        /// </summary>
        public void SetFullMatchingTgsCategory(TgsHlaTypingCategory tgsCategory)
        {
            metaDonorSelectionCriteria.MatchingTgsTypingCategories.A_1 = tgsCategory;
            metaDonorSelectionCriteria.MatchingTgsTypingCategories.A_2 = tgsCategory;
            metaDonorSelectionCriteria.MatchingTgsTypingCategories.B_1 = tgsCategory;
            metaDonorSelectionCriteria.MatchingTgsTypingCategories.B_2 = tgsCategory;
            metaDonorSelectionCriteria.MatchingTgsTypingCategories.C_1 = tgsCategory;
            metaDonorSelectionCriteria.MatchingTgsTypingCategories.C_2 = tgsCategory;
            metaDonorSelectionCriteria.MatchingTgsTypingCategories.DQB1_1 = tgsCategory;
            metaDonorSelectionCriteria.MatchingTgsTypingCategories.DQB1_2 = tgsCategory;
            metaDonorSelectionCriteria.MatchingTgsTypingCategories.DRB1_1 = tgsCategory;
            metaDonorSelectionCriteria.MatchingTgsTypingCategories.DRB1_2 = tgsCategory;

            //There is no DPB1 test data with fewer than 4 fields
            metaDonorSelectionCriteria.MatchingTgsTypingCategories.DPB1_1 = TgsHlaTypingCategory.FourFieldAllele;
            metaDonorSelectionCriteria.MatchingTgsTypingCategories.DPB1_2 = TgsHlaTypingCategory.FourFieldAllele;
        }

        public void SetMatchingDonorUntypedAtLocus(Locus locus)
        {
            databaseDonorSelectionCriteria.MatchingTypingResolutions.SetAtLocus(locus, TypePositions.Both, HlaTypingResolution.Untyped);
        }

        public void SetAsMatchLevelAtAllLoci(MatchLevel matchLevel)
        {
            var matchLevels = new PhenotypeInfo<int>().Map((l, p, level) => matchLevel);
            SetMatchLevels(matchLevels);
        }

        public void SetMatchingRegistry(RegistryCode registry)
        {
            metaDonorSelectionCriteria.MatchingRegistry = registry;
        }

        public void SetMatchingDonorType(DonorType donorType)
        {
            metaDonorSelectionCriteria.MatchingDonorType = donorType;
        }

        public PhenotypeInfo<string> GetPatientHla()
        {
            return patientHlaSelector.GetPatientHla(GetMetaDonor(), patientHlaSelectionCriteria);
        }

        public int GetExpectedMatchingDonorId()
        {
            return databaseDonorSelector.GetExpectedMatchingDonorId(GetMetaDonor(), databaseDonorSelectionCriteria);
        }

        private MetaDonor GetMetaDonor()
        {
            // Cache the selected meta-donor to ensure we do not have to perform this calculation multiple times
            if (selectedMetaDonor == null)
            {
                selectedMetaDonor = metaDonorSelector.GetMetaDonor(metaDonorSelectionCriteria);
            }

            return selectedMetaDonor;
        }

        /// <summary>
        /// This method should be used to set the expected match levels for meta donor and patient hla selction criteria,
        /// as both criteria rely on this data
        /// </summary>
        private void SetMatchLevels(PhenotypeInfo<MatchLevel> matchLevels)
        {
            metaDonorSelectionCriteria.MatchLevels = matchLevels;
            patientHlaSelectionCriteria.MatchLevels = matchLevels;
        }
    }
}