using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Exceptions;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Models;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Models.Hla;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Models.PatientDataSelection;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Services.PatientDataSelection.DataSelectors;
using static EnumStringValues.EnumExtensions;

namespace Atlas.MatchingAlgorithm.Test.Validation.TestData.Services.PatientDataSelection.PatientFactories
{
    /// <summary>
    /// Stores various search criteria from the feature file, and selects appropriate patient data.
    /// The criteria will map to exactly one expected donor from the database
    /// e.g. A 9/10 adult match with mismatch at A, at NMDP code resolution
    /// </summary>
    public interface IPatientDataFactory
    {
        // Patient only criteria
        void SetAsSixOutOfSixMatch();
        void SetAsEightOutOfEightMatch();
        void SetAsTenOutOfTenMatch();
        void SetMismatchAtPosition(Locus locus, LocusPosition position);
        void SetPatientUntypedAtLocus(Locus locus);
        void SetPatientTypingResolutionAtLocus(Locus locus, HlaTypingResolution resolution);
        void SetMatchOrientationAtLocus(Locus locus, MatchOrientation orientation);
        void SetPatientNonMatchingNullAlleleAtPosition(Locus locus, LocusPosition position);

        // Meta-donor and patient criteria
        void SetAsMatchLevelAtAllLoci(MatchLevel matchLevel);
        void SetPatientHomozygousAtLocus(Locus locus);

        // Meta-donor only criteria
        void SetMatchingDonorType(DonorType donorType);
        void SetMatchingDonorHomozygousAtLocus(Locus locus);

        /// <summary>
        /// Will set the desired tgs typing category at all positions
        /// </summary>
        void SetFullMatchingTgsCategory(TgsHlaTypingCategory tgsCategory);

        void SetAlleleStringShouldContainDifferentGroupsAtLocus(Locus locus);
        void SetHasNonNullExpressionSuffixAtLocus(Locus locus);
        void SetHasNullAlleleAtPosition(Locus locus, LocusPosition position);

        // Meta-donor and database-donor criteria

        /// <summary>
        /// Adds an expected database donor at the given resolution
        /// Will assume database donor matches genotype at all positions
        /// </summary>
        void AddFullDonorTypingResolution(PhenotypeInfo<HlaTypingResolution> resolutions);

        /// <summary>
        /// Adds an expected database donor with the given criteria
        /// </summary>
        void AddExpectedDatabaseDonor(DatabaseDonorSpecification databaseDonorSpecification);

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
    }

    public class PatientDataFactory : IPatientDataFactory, IPatientDataProvider, IExpectedDonorProvider
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
            DatabaseDonorDetailsSets = new List<DatabaseDonorSpecification>
            {
                new DatabaseDonorSpecification {MatchingTypingResolutions = new PhenotypeInfo<HlaTypingResolution>(DefaultTypingResolution)}
            },
        };

        private readonly List<DatabaseDonorSpecification> databaseDonorSelectionCriteriaSet = new List<DatabaseDonorSpecification>
        {
            // TODO: Do not always assume a fully TGS typed match?
            new DatabaseDonorSpecification
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
            var mismatchedLoci = new[] {Locus.C, Locus.Dqb1, Locus.Dpb1}.ToList();
            patientHlaSelectionCriteria.HlaSources = new PhenotypeInfo<bool>().Map((locus, p, noop) => mismatchedLoci.Contains(locus)
                ? PatientHlaSource.ExpressingAlleleMismatch
                : PatientHlaSource.Match);
        }

        public void SetAsEightOutOfEightMatch()
        {
            var mismatchedLoci = new[] {Locus.Dqb1, Locus.Dpb1}.ToList();
            patientHlaSelectionCriteria.HlaSources = new PhenotypeInfo<bool>().Map((locus, p, noop) => mismatchedLoci.Contains(locus)
                ? PatientHlaSource.ExpressingAlleleMismatch
                : PatientHlaSource.Match);
        }

        public void SetAsTenOutOfTenMatch()
        {
            patientHlaSelectionCriteria.HlaSources = new PhenotypeInfo<bool>().Map((locus, p, noop) =>
                locus == Locus.Dpb1 ? PatientHlaSource.ExpressingAlleleMismatch : PatientHlaSource.Match);
        }

        public void SetMismatchAtPosition(Locus locus, LocusPosition position)
        {
            patientHlaSelectionCriteria.HlaSources =
                patientHlaSelectionCriteria.HlaSources.SetPosition(locus, position, PatientHlaSource.ExpressingAlleleMismatch);
        }

        public void SetPatientUntypedAtLocus(Locus locus)
        {
            SetPatientTypingResolutionAtLocus(locus, HlaTypingResolution.Untyped);
        }

        public void SetPatientTypingResolutionAtLocus(Locus locus, HlaTypingResolution resolution)
        {
            patientHlaSelectionCriteria.PatientTypingResolutions = patientHlaSelectionCriteria.PatientTypingResolutions.SetLocus(locus, resolution);
        }

        public void SetMatchOrientationAtLocus(Locus locus, MatchOrientation orientation)
        {
            patientHlaSelectionCriteria.Orientations = patientHlaSelectionCriteria.Orientations.SetLocus(locus, orientation);
        }

        public void SetPatientNonMatchingNullAlleleAtPosition(Locus locus, LocusPosition position)
        {
            patientHlaSelectionCriteria.HlaSources =
                patientHlaSelectionCriteria.HlaSources.SetPosition(locus, position, PatientHlaSource.NullAlleleMismatch);
        }

        #endregion

        #region Meta-donor only criteria

        public void SetMatchingDonorType(DonorType donorType)
        {
            metaDonorSelectionCriteria.MatchingDonorType = donorType;
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
            metaDonorSelectionCriteria.AlleleStringContainsDifferentAntigenGroups =
                metaDonorSelectionCriteria.AlleleStringContainsDifferentAntigenGroups.SetLocus(locus, true);
        }

        public void SetHasNonNullExpressionSuffixAtLocus(Locus locus)
        {
            metaDonorSelectionCriteria.HasNonNullExpressionSuffix = metaDonorSelectionCriteria.HasNonNullExpressionSuffix.SetLocus(locus, true);
        }

        public void SetHasNullAlleleAtPosition(Locus locus, LocusPosition position)
        {
            metaDonorSelectionCriteria.IsNullExpressing = metaDonorSelectionCriteria.IsNullExpressing.SetPosition(locus, position, true);
        }

        public void SetMatchingDonorHomozygousAtLocus(Locus locus)
        {
            metaDonorSelectionCriteria.IsHomozygous = metaDonorSelectionCriteria.IsHomozygous.SetLocus(locus, true);
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
            var matchesAtLocus = patientHlaSelectionCriteria.HlaMatches.GetLocus(locus);
            if (matchesAtLocus.Position1 && matchesAtLocus.Position2)
            {
                // For an exact match to exist, if the patient is homozygous the donor must implicitly also be homozygous
                SetMatchingDonorHomozygousAtLocus(locus);
            }

            patientHlaSelectionCriteria.IsHomozygous = patientHlaSelectionCriteria.IsHomozygous.SetLocus(locus, true);

            // For a homozygous locus, typing resolution must be single allele (TGS)
            SetPatientTypingResolutionAtLocus(locus, HlaTypingResolution.Tgs);
        }

        #endregion

        #region Meta-donor and database donor criteria

        public void AddFullDonorTypingResolution(PhenotypeInfo<HlaTypingResolution> resolutions)
        {
            var databaseDonorDetails = new DatabaseDonorSpecification
            {
                MatchingTypingResolutions = resolutions
            };
            AddExpectedDatabaseDonor(databaseDonorDetails);
        }

        public void AddExpectedDatabaseDonor(DatabaseDonorSpecification databaseDonorSpecification)
        {
            metaDonorSelectionCriteria.DatabaseDonorDetailsSets.Add(databaseDonorSpecification);
            databaseDonorSelectionCriteriaSet.Add(databaseDonorSpecification);
        }

        public void UpdateMatchingDonorTypingResolutionsAtLocus(Locus locus, HlaTypingResolution resolution)
        {
            foreach (var resolutionSet in metaDonorSelectionCriteria.DatabaseDonorDetailsSets)
            {
                resolutionSet.MatchingTypingResolutions = resolutionSet.MatchingTypingResolutions.SetLocus(locus, resolution);
            }

            foreach (var databaseDonorSelectionCriteria in databaseDonorSelectionCriteriaSet)
            {
                databaseDonorSelectionCriteria.MatchingTypingResolutions =
                    databaseDonorSelectionCriteria.MatchingTypingResolutions.SetLocus(locus, resolution);
            }
        }

        public void UpdateDonorGenotypeMatchGetPosition(Locus locus, LocusPosition position, bool shouldMatchGenotype)
        {
            foreach (var resolutionSet in metaDonorSelectionCriteria.DatabaseDonorDetailsSets)
            {
                resolutionSet.ShouldMatchGenotype = resolutionSet.ShouldMatchGenotype.SetPosition(locus, position, shouldMatchGenotype);
            }

            foreach (var databaseDonorSelectionCriteria in databaseDonorSelectionCriteriaSet)
            {
                databaseDonorSelectionCriteria.ShouldMatchGenotype =
                    databaseDonorSelectionCriteria.ShouldMatchGenotype.SetPosition(locus, position, shouldMatchGenotype);
            }
        }

        public void UpdateMatchingDonorTypingResolutionsAtAllLoci(HlaTypingResolution resolution)
        {
            foreach (var locus in EnumerateValues<Locus>())
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
        /// If there are any logical inconsistencies in the criteria specified, they should be raised here as an Exception to aid debugging
        /// </summary>
        private void ValidateCriteria()
        {
            patientHlaSelectionCriteria.MatchLevels.EachPosition((l, p, matchLevel) =>
            {
                if (matchLevel == MatchLevel.FirstThreeFieldAllele
                    && metaDonorSelectionCriteria.MatchingTgsTypingCategories.GetPosition(l, p) != TgsHlaTypingCategory.FourFieldAllele)
                {
                    throw new InvalidTestDataException(
                        "Cannot generate data for a patient with a three field (not fourth field) match if the matching donor is not four field TGS typed");
                }

                if (matchLevel == MatchLevel.FirstTwoFieldAllele)
                {
                    var tgsTypingCategory = metaDonorSelectionCriteria.MatchingTgsTypingCategories.GetPosition(l, p);
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