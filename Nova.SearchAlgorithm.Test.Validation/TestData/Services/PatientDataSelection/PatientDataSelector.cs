using System;
using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models.Hla;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models.PatientDataSelection;
using Nova.SearchAlgorithm.Test.Validation.TestData.Repositories;
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

        private readonly IAlleleRepository alleleRepository;
        private readonly IMetaDonorSelector metaDonorSelector;
        private readonly IDatabaseDonorSelector databaseDonorSelector;
        private MetaDonor selectedMetaDonor;

        private PhenotypeInfo<bool> HlaMatches { get; set; } = new PhenotypeInfo<bool>();

        // TODO: NOVA-1642 - patient typing resolutions to be set by step; currently defaulting to TGS
        private readonly PhenotypeInfo<HlaTypingResolution> patientTypingCategories = new PhenotypeInfo<HlaTypingResolution>
        {
            A_1 = HlaTypingResolution.Tgs,
            A_2 = HlaTypingResolution.Tgs,
            B_1 = HlaTypingResolution.Tgs,
            B_2 = HlaTypingResolution.Tgs,
            C_1 = HlaTypingResolution.Tgs,
            C_2 = HlaTypingResolution.Tgs,
            DPB1_1 = HlaTypingResolution.Tgs,
            DPB1_2 = HlaTypingResolution.Tgs,
            DQB1_1 = HlaTypingResolution.Tgs,
            DQB1_2 = HlaTypingResolution.Tgs,
            DRB1_1 = HlaTypingResolution.Tgs,
            DRB1_2 = HlaTypingResolution.Tgs,
        };

        private readonly MetaDonorSelectionCriteria metaDonorSelectionCriteria = new MetaDonorSelectionCriteria();
        private readonly DatabaseDonorSelectionCriteria databaseDonorSelectionCriteria = new DatabaseDonorSelectionCriteria();

        public PatientDataSelector(
            IAlleleRepository alleleRepository,
            IMetaDonorSelector metaDonorSelector,
            IDatabaseDonorSelector databaseDonorSelector
        )
        {
            this.alleleRepository = alleleRepository;
            this.metaDonorSelector = metaDonorSelector;
            this.databaseDonorSelector = databaseDonorSelector;
        }

        public void SetPatientUntypedAt(Locus locus)
        {
            patientTypingCategories.SetAtLocus(locus, TypePositions.Both, HlaTypingResolution.Untyped);
        }

        public void SetAsTenOutOfTenMatch()
        {
            HlaMatches = HlaMatches.Map((l, p, hasMatch) => l != Locus.Dpb1);
        }

        public void SetAsEightOutOfEightMatch()
        {
            HlaMatches = HlaMatches.Map((l, p, hasMatch) => l != Locus.Dqb1 || l != Locus.Dpb1);
        }

        public void SetAsSixOutOfSixMatch()
        {
            HlaMatches = HlaMatches.Map((l, p, hasMatch) => l != Locus.C || l != Locus.Dqb1 || l != Locus.Dpb1);
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
            metaDonorSelectionCriteria.MatchLevels.A_1 = matchLevel;
            metaDonorSelectionCriteria.MatchLevels.A_2 = matchLevel;
            metaDonorSelectionCriteria.MatchLevels.B_1 = matchLevel;
            metaDonorSelectionCriteria.MatchLevels.B_2 = matchLevel;
            metaDonorSelectionCriteria.MatchLevels.C_1 = matchLevel;
            metaDonorSelectionCriteria.MatchLevels.C_2 = matchLevel;
            metaDonorSelectionCriteria.MatchLevels.DPB1_1 = matchLevel;
            metaDonorSelectionCriteria.MatchLevels.DPB1_2 = matchLevel;
            metaDonorSelectionCriteria.MatchLevels.DQB1_1 = matchLevel;
            metaDonorSelectionCriteria.MatchLevels.DQB1_2 = matchLevel;
            metaDonorSelectionCriteria.MatchLevels.DRB1_1 = matchLevel;
            metaDonorSelectionCriteria.MatchLevels.DRB1_2 = matchLevel;
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
            return GetMetaDonor().Genotype.Hla.Map(GetHlaName);
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

        private string GetHlaName(Locus locus, TypePositions position, TgsAllele tgsAllele)
        {
            var allele = GetTgsAllele(locus, position, tgsAllele);
            var typingCategory = patientTypingCategories.DataAtPosition(locus, position);

            return allele.GetHlaForCategory(typingCategory);
        }

        private TgsAllele GetTgsAllele(Locus locus, TypePositions position, TgsAllele originalAllele)
        {
            // if patient should be mismatched at this position
            if (!HlaMatches.DataAtPosition(locus, position))
            {
                return GenotypeGenerator.NonMatchingGenotype.Hla.DataAtPosition(locus, position);
            }

            // if patient should have a P-group match at this position
            if (metaDonorSelectionCriteria.MatchLevels.DataAtPosition(locus, position) == MatchLevel.PGroup)
            {
                return GetDifferentTgsAlleleFromSamePGroup(locus, originalAllele, position);
            }

            return originalAllele;
        }

        private TgsAllele GetDifferentTgsAlleleFromSamePGroup(Locus locus, TgsAllele allele, TypePositions position)
        {
            var allelesAtLocus = alleleRepository.FourFieldAllelesWithNonUniquePGroups().DataAtLocus(locus);
            var allAllelesAtLocus = allelesAtLocus.Item1.Concat(allelesAtLocus.Item2).ToList();
            var pGroup = allAllelesAtLocus.First(a => a.AlleleName == allele.TgsTypedAllele).PGroup;
            var selectedAllele = allAllelesAtLocus.First(a =>
                a.PGroup == pGroup
                && a.AlleleName != allele.TgsTypedAllele
                && a.AlleleName != GetMetaDonor().Genotype.Hla.DataAtPosition(locus, position.Other()).TgsTypedAllele);

            return TgsAllele.FromFourFieldAllele(selectedAllele, locus);
        }
    }
}