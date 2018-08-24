using System;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Exceptions;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models.Hla;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models.PatientDataSelection;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Services.PatientDataSelection
{
    public interface IPatientDataSelector
    {
        void SetAsSixOutOfSixMatch();
        void SetAsEightOutOfEightMatch();
        void SetAsTenOutOfTenMatch();
        void SetMismatchesAtLocus(int numberOfMismatches, Locus locus);

        void SetMatchingDonorType(DonorType donorType);
        void SetMatchingRegistry(RegistryCode registry);

        void SetAsMatchLevelAtAllLoci(MatchLevel matchLevel);
        void SetFullMatchingTypingResolution(HlaTypingResolution resolution);
        void SetMatchingTypingResolutionAtLocus(Locus locus, HlaTypingResolution resolution);
        void SetFullMatchingTgsCategory(TgsHlaTypingCategory tgsCategory);
        void SetMatchingDonorUntypedAtLocus(Locus locus);
        void SetMatchingDonorHomozygousAtLocus(Locus locus);

        void SetPatientUntypedAtLocus(Locus locus);
        void SetPatientTypingResolutionAtLocus(Locus locus, HlaTypingResolution resolution);
        void SetPatientHomozygousAtLocus(Locus locus);

        PhenotypeInfo<string> GetPatientHla();
        int GetExpectedMatchingDonorId();
    }

    /// <summary>
    /// Stores various search criteria from the feature file, and selects appropriate patient data
    /// e.g. A 9/10 adult match with mismatch at A, from AN registry
    /// </summary>
    public class PatientDataSelector : IPatientDataSelector
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
            TypingResolutions = DefaultTypingResolutions
        };

        private readonly DatabaseDonorSelectionCriteria databaseDonorSelectionCriteria =
            new DatabaseDonorSelectionCriteria {MatchingTypingResolutions = DefaultTypingResolutions};

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

        public void SetAsSixOutOfSixMatch()
        {
            var matches = new PhenotypeInfo<bool>().Map((locus, p, noop) => locus != Locus.C || locus != Locus.Dqb1 || locus != Locus.Dpb1);
            patientHlaSelectionCriteria.HlaMatches = matches;
        }

        public void SetAsEightOutOfEightMatch()
        {
            var matches = new PhenotypeInfo<bool>().Map((locus, p, noop) => locus != Locus.Dqb1 || locus != Locus.Dpb1);
            patientHlaSelectionCriteria.HlaMatches = matches;
        }

        public void SetAsTenOutOfTenMatch()
        {
            var matches = new PhenotypeInfo<bool>().Map((locus, p, noop) => locus != Locus.Dpb1);
            patientHlaSelectionCriteria.HlaMatches = matches;
        }

        public void SetMismatchesAtLocus(int numberOfMismatches, Locus locus)
        {
            switch (numberOfMismatches)
            {
                case 1:
                    patientHlaSelectionCriteria.HlaMatches.SetAtLocus(locus, TypePositions.One, false);
                    break;
                case 2:
                    patientHlaSelectionCriteria.HlaMatches.SetAtLocus(locus, TypePositions.Both, false);
                    break;
                case 0:
                    break;
                default:
                    throw new Exception("Cannot have fewer than 0 or more than 2 mismatches");
            }
        }

        public void SetMatchingDonorType(DonorType donorType)
        {
            metaDonorSelectionCriteria.MatchingDonorType = donorType;
        }

        public void SetMatchingRegistry(RegistryCode registry)
        {
            metaDonorSelectionCriteria.MatchingRegistry = registry;
        }

        public void SetAsMatchLevelAtAllLoci(MatchLevel matchLevel)
        {
            var matchLevels = new PhenotypeInfo<MatchLevel>(matchLevel);
            SetMatchLevels(matchLevels);
        }

        /// <summary>
        /// Will set the desired typing resolution at all positions
        /// </summary>
        public void SetFullMatchingTypingResolution(HlaTypingResolution resolution)
        {
            var hlaTypingResolutions = new PhenotypeInfo<HlaTypingResolution>(resolution);
            SetTypingResolutions(hlaTypingResolutions);
        }

        public void SetMatchingTypingResolutionAtLocus(Locus locus, HlaTypingResolution resolution)
        {
            var typingResolutions = metaDonorSelectionCriteria.TypingResolutions;
            SetTypingResolutions(typingResolutions.Map((l, p, existingResolution) => l == locus ? resolution : existingResolution));
        }

        /// <summary>
        /// Will set the desired tgs typing category at all positions
        /// </summary>
        public void SetFullMatchingTgsCategory(TgsHlaTypingCategory tgsCategory)
        {
            var categories = new PhenotypeInfo<TgsHlaTypingCategory>(tgsCategory);
            metaDonorSelectionCriteria.MatchingTgsTypingCategories = categories;
        }

        public void SetMatchingDonorUntypedAtLocus(Locus locus)
        {
            databaseDonorSelectionCriteria.MatchingTypingResolutions.SetAtLocus(locus, TypePositions.Both, HlaTypingResolution.Untyped);
        }

        public void SetMatchingDonorHomozygousAtLocus(Locus locus)
        {
            metaDonorSelectionCriteria.IsHomozygous.SetAtLocus(locus, true);
        }

        public void SetPatientUntypedAtLocus(Locus locus)
        {
            SetPatientTypingResolutionAtLocus(locus, HlaTypingResolution.Untyped);
        }

        public void SetPatientTypingResolutionAtLocus(Locus locus, HlaTypingResolution resolution)
        {
            patientHlaSelectionCriteria.PatientTypingResolutions.SetAtLocus(locus, TypePositions.Both, resolution);
        }

        public void SetPatientHomozygousAtLocus(Locus locus)
        {
            var matchesAtLocus = patientHlaSelectionCriteria.HlaMatches.DataAtLocus(locus);
            if (matchesAtLocus.Item1 && matchesAtLocus.Item2)
            {
                // For an exact match to exist, if the patient is homozygous the donor must implicitly also be homozygous
                // TODO: NOVA-1188: This assumption is not true when considering null alleles. Update when null matching is implemented
                SetMatchingDonorHomozygousAtLocus(locus);
            }

            patientHlaSelectionCriteria.IsHomozygous.SetAtLocus(locus, true);

            // For a homozygous locus, typing resolution must be single allele (TGS)
            SetPatientTypingResolutionAtLocus(locus, HlaTypingResolution.Tgs);
        }

        public PhenotypeInfo<string> GetPatientHla()
        {
            ValidateCriteria();
            return patientHlaSelector.GetPatientHla(GetMetaDonor(), patientHlaSelectionCriteria);
        }

        public int GetExpectedMatchingDonorId()
        {
            return databaseDonorSelector.GetExpectedMatchingDonorId(GetMetaDonor(), databaseDonorSelectionCriteria);
        }

        /// <summary>
        /// Should only be called when all criteria are set up.
        /// If there are any logical inconsitencies in the criteria specified, they should be raised here as an Exception to aid debugging
        /// </summary>
        private void ValidateCriteria()
        {
            patientHlaSelectionCriteria.MatchLevels.EachPosition((l, p, matchLevel) =>
            {
                if (matchLevel == MatchLevel.FirstThreeFieldAllele
                    && metaDonorSelectionCriteria.MatchingTgsTypingCategories.DataAtPosition(l, p) != TgsHlaTypingCategory.FourFieldAllele)
                {
                    throw new InvalidTestDataException(
                        "Cannot generate data for a patient with a three field (not fourth field) match if the matching donor is not four field TGS typed");
                }

                if (matchLevel == MatchLevel.FirstTwoFieldAllele)
                {
                    var tgsTypingCategory = metaDonorSelectionCriteria.MatchingTgsTypingCategories.DataAtPosition(l, p);
                    if (tgsTypingCategory == TgsHlaTypingCategory.FourFieldAllele)
                    {
                        throw new InvalidTestDataException(
                            "No test data has been curated for four-field alleles matching at the first two fields only. Please add this test data if necessary");
                    }

                    if (tgsTypingCategory != TgsHlaTypingCategory.ThreeFieldAllele)
                    {
                        throw new InvalidTestDataException(
                            "Cannot generate data for a patient with a two field match if the matching donor is not guaranteed to have at least three fields");
                    }
                }
            });
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
        /// This method should be used to set the expected match levels for meta donor and patient hla selction criteria,
        /// as both criteria rely on this data
        /// </summary>
        private void SetMatchLevels(PhenotypeInfo<MatchLevel> matchLevels)
        {
            metaDonorSelectionCriteria.MatchLevels = matchLevels;
            patientHlaSelectionCriteria.MatchLevels = matchLevels;
        }

        /// <summary>
        /// This method should be used to set the expected typing resolutions for meta donor and database donor selction criteria,
        /// as both criteria rely on this data
        /// </summary>
        private void SetTypingResolutions(PhenotypeInfo<HlaTypingResolution> resolutions)
        {
            metaDonorSelectionCriteria.TypingResolutions = resolutions;
            databaseDonorSelectionCriteria.MatchingTypingResolutions = resolutions;
        }
    }
}