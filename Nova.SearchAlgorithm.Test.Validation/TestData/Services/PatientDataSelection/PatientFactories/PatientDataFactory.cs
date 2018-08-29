using System;
using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Exceptions;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models.Hla;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models.PatientDataSelection;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Services.PatientDataSelection
{
    /// <summary>
    /// Stores various search criteria from the feature file, and selects appropriate patient data.
    /// The criteria will map to exactly one expected donor from the database
    /// e.g. A 9/10 adult match with mismatch at A, from AN registry, at NMDP resolution
    /// </summary>
    public interface IPatientDataFactory
    {
        // Patient only criteria
        void SetAsSixOutOfSixMatch();
        void SetAsEightOutOfEightMatch();
        void SetAsTenOutOfTenMatch();
        void SetMismatchesAtLocus(int numberOfMismatches, Locus locus);
        void SetPatientUntypedAtLocus(Locus locus);
        void SetPatientTypingResolutionAtLocus(Locus locus, HlaTypingResolution resolution);

        // Meta-donor and patient criteria
        void SetAsMatchLevelAtAllLoci(MatchLevel matchLevel);
        void SetPatientHomozygousAtLocus(Locus locus);

        // Meta-donor only criteria
        void SetMatchingDonorType(DonorType donorType);
        void SetMatchingRegistry(RegistryCode registry);
        void SetMatchingDonorHomozygousAtLocus(Locus locus);
        /// <summary>
        /// Will set the desired tgs typing category at all positions
        /// </summary>
        void SetFullMatchingTgsCategory(TgsHlaTypingCategory tgsCategory);
        void SetNumberOfMetaDonorsToSkip(int numberToSkip);
        void SetAlleleStringShouldContainDifferentGroupsAtLocus(Locus locus);

        // Meta-donor and database-donor criteria
        void AddFullDonorTypingResolution(PhenotypeInfo<HlaTypingResolution> resolutions);
        /// <summary>
        /// Will update all expected matching donor resolutions at the specified locus.
        /// This is intended for use with a single matching donor.
        /// Be careful that this is definitely what you want if matching multiple donors
        /// </summary>
        void UpdateMatchingDonorTypingResolutionsAtLocus(Locus locus, HlaTypingResolution resolution);
        /// <summary>
        /// Will update all expected matching donor resolutions, at all loci.
        /// This is intended for use with a single matching donor.
        /// Be careful that this is definitely what you want if matching multiple donors
        /// </summary>
        void UpdateMatchingDonorTypingResolutionsAtAllLoci(HlaTypingResolution resolution);
        
        // Selected Data
        IEnumerable<int> GetExpectedMatchingDonorIds();
        PhenotypeInfo<string> GetPatientHla();
    }

    public class PatientDataFactory : IPatientDataFactory
    {
        private readonly IMetaDonorSelector metaDonorSelector;
        private readonly IDatabaseDonorSelector databaseDonorSelector;
        private readonly IPatientHlaSelector patientHlaSelector;

        private MetaDonor selectedMetaDonor;

        private const MatchLevel DefaultMatchLevel = MatchLevel.Allele;
        private const HlaTypingResolution DefaultTypingResolution = HlaTypingResolution.Tgs;

        private readonly MetaDonorSelectionCriteria metaDonorSelectionCriteria = new MetaDonorSelectionCriteria
        {
            MatchLevels = new PhenotypeInfo<MatchLevel>(DefaultMatchLevel),
            TypingResolutionSets = new List<PhenotypeInfo<HlaTypingResolution>> {new PhenotypeInfo<HlaTypingResolution>(DefaultTypingResolution)},
        };

        private readonly List<DatabaseDonorSelectionCriteria> databaseDonorSelectionCriteriaSet = new List<DatabaseDonorSelectionCriteria>
        {
            new DatabaseDonorSelectionCriteria
            {
                MatchingTypingResolutions = new PhenotypeInfo<HlaTypingResolution>(DefaultTypingResolution)
            },
        };

        private readonly PatientHlaSelectionCriteria patientHlaSelectionCriteria = new PatientHlaSelectionCriteria
        {
            MatchLevels = new PhenotypeInfo<MatchLevel>(DefaultMatchLevel)
        };

        public PatientDataFactory(
            IMetaDonorSelector metaDonorSelector,
            IDatabaseDonorSelector databaseDonorSelector,
            IPatientHlaSelector patientHlaSelector
        )
        {
            this.metaDonorSelector = metaDonorSelector;
            this.databaseDonorSelector = databaseDonorSelector;
            this.patientHlaSelector = patientHlaSelector;
        }
        
        #region Patient only critera

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
            // TODO: NOVA-1713: Allow mismatches to be specified by locus
            switch (numberOfMismatches)
            {
                case 1:
                    patientHlaSelectionCriteria.HlaMatches.SetAtPosition(locus, TypePositions.One, false);
                    break;
                case 2:
                    patientHlaSelectionCriteria.HlaMatches.SetAtPosition(locus, TypePositions.Both, false);
                    break;
                case 0:
                    break;
                default:
                    throw new Exception("Cannot have fewer than 0 or more than 2 mismatches");
            }
        }
        
        public void SetPatientUntypedAtLocus(Locus locus)
        {
            SetPatientTypingResolutionAtLocus(locus, HlaTypingResolution.Untyped);
        }

        public void SetPatientTypingResolutionAtLocus(Locus locus, HlaTypingResolution resolution)
        {
            patientHlaSelectionCriteria.PatientTypingResolutions.SetAtLocus(locus, resolution);
        }

        #endregion

        #region Meta-donor only criteria
        
        public void SetMatchingDonorType(DonorType donorType)
        {
            metaDonorSelectionCriteria.MatchingDonorType = donorType;
        }

        public void SetMatchingRegistry(RegistryCode registry)
        {
            metaDonorSelectionCriteria.MatchingRegistry = registry;
        }
        
        public void SetFullMatchingTgsCategory(TgsHlaTypingCategory tgsCategory)
        {
            var categories = new PhenotypeInfo<TgsHlaTypingCategory>(tgsCategory);
            metaDonorSelectionCriteria.MatchingTgsTypingCategories = categories;
        }

        public void SetNumberOfMetaDonorsToSkip(int numberToSkip)
        {
            metaDonorSelectionCriteria.MetaDonorsToSkip = numberToSkip;
        }

        public void SetAlleleStringShouldContainDifferentGroupsAtLocus(Locus locus)
        {
            metaDonorSelectionCriteria.AlleleStringContainsDifferentAntigenGroups.SetAtLocus(locus, true);
        }

        public void SetMatchingDonorHomozygousAtLocus(Locus locus)
        {
            metaDonorSelectionCriteria.IsHomozygous.SetAtLocus(locus, true);
        }
        
        #endregion

        #region Meta-donor and Patient criteria

        public void SetAsMatchLevelAtAllLoci(MatchLevel matchLevel)
        {
            var matchLevels = new PhenotypeInfo<MatchLevel>(matchLevel);
            SetMatchLevels(matchLevels);
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

        #endregion

        #region Meta-donor and database donor criteria

        public void AddFullDonorTypingResolution(PhenotypeInfo<HlaTypingResolution> resolutions)
        {
            metaDonorSelectionCriteria.TypingResolutionSets.Add(resolutions);
            databaseDonorSelectionCriteriaSet.Add(new DatabaseDonorSelectionCriteria
            {
                MatchingTypingResolutions = resolutions,
            });
        }

        public void UpdateMatchingDonorTypingResolutionsAtLocus(Locus locus, HlaTypingResolution resolution)
        {
            foreach (var resolutionSet in metaDonorSelectionCriteria.TypingResolutionSets)
            {
                resolutionSet.SetAtLocus(locus, resolution);
            }

            foreach (var databaseDonorSelectionCriteria in databaseDonorSelectionCriteriaSet)
            {
                databaseDonorSelectionCriteria.MatchingTypingResolutions.SetAtLocus(locus, resolution);
            }
        }

        public void UpdateMatchingDonorTypingResolutionsAtAllLoci(HlaTypingResolution resolution)
        {
            foreach (var locus in LocusHelpers.AllLoci())
            {
                UpdateMatchingDonorTypingResolutionsAtLocus(locus, resolution);
            }
        }

        #endregion

        #region Selected Data

        public PhenotypeInfo<string> GetPatientHla()
        {
            ValidateCriteria();
            return patientHlaSelector.GetPatientHla(GetMetaDonor(), patientHlaSelectionCriteria);
        }

        public IEnumerable<int> GetExpectedMatchingDonorIds()
        {
            return databaseDonorSelectionCriteriaSet.Select(c => databaseDonorSelector.GetExpectedMatchingDonorId(GetMetaDonor(), c));
        }

        #endregion

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