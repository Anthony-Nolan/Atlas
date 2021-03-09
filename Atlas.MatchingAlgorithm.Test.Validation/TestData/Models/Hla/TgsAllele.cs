using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData.Hla.Services.AlleleNameUtils;
using Atlas.Common.Maths;
using Atlas.Common.Utils.Extensions;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Exceptions;

namespace Atlas.MatchingAlgorithm.Test.Validation.TestData.Models.Hla
{
    /// <summary>
    /// Test data will be generated from strongly types TGS data - i.e. in a 2, 3, or 4 field allele name format
    /// It will be 'dumbed down' to lower resolution typings. This class includes all lower resolution typings for a given 3 or 4 field allele.
    /// This should be just a data model, and so any lookups for lower resolutions will need to be provided at creation
    /// i.e. Serology, NMDP code, PGroup/GGroup (if necessary), other alleles to use in allele string
    /// </summary>
    public class TgsAllele
    {
        private const string AlleleNamePrefix = "*";
        private const string AlleleSeparator = "/";

        /// <summary>
        /// Creates a new TGS allele from test data source
        /// </summary>
        /// <param name="allele">
        ///     The test data to use when creating this allele model
        ///     Should contain Serology and NMDP code if these resolutions are to be used
        /// </param>
        /// <param name="alleleStringOptions">Alleles to use in the various generated allele strings</param>
        public static TgsAllele FromTestDataAllele(AlleleTestData allele, AlleleStringOptions alleleStringOptions)
        {
            TgsAllele tgsAllele;

            var fieldCount = AlleleSplitter.NumberOfFields(allele.AlleleName);
            switch (fieldCount)
            {
                case 4:
                    tgsAllele = FromFourFieldAllele(allele, alleleStringOptions.SubtypeString);
                    break;
                case 3:
                    tgsAllele = FromThreeFieldAllele(allele, alleleStringOptions.SubtypeString);
                    break;
                case 2:
                    tgsAllele = FromTwoFieldAllele(allele, alleleStringOptions.SubtypeString);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("TGS test allele of unexpected field count found: " + allele.AlleleName);
            }

            tgsAllele.AlleleStringOfNamesWithMultiplePGroups = GenerateAlleleStringOfNames(allele, alleleStringOptions.NameStringWithMultiplePGroups);
            tgsAllele.AlleleStringOfNamesWithSinglePGroup = GenerateAlleleStringOfNames(allele, alleleStringOptions.NameStringWithSinglePGroup);
            tgsAllele.AlleleStringOfNames = GenerateAlleleStringOfNames(allele, alleleStringOptions.NameString);
            return tgsAllele;
        }

        /// <summary>
        /// Create TGS Allele from test data source without setting the allele string properties.
        /// </summary>
        public static TgsAllele FromTestDataAllele(AlleleTestData allele)
        {
            return FromTestDataAllele(allele, new AlleleStringOptions());
        }

        private static TgsAllele FromFourFieldAllele(AlleleTestData fourFieldAllele, List<AlleleTestData> otherAllelesInSubtypeString)
        {
            var threeFieldAllele = new AlleleTestData
            {
                AlleleName = AlleleSplitter.RemoveLastField(fourFieldAllele.AlleleName),
                PGroup = fourFieldAllele.PGroup,
                GGroup = fourFieldAllele.GGroup,
                NmdpCode = fourFieldAllele.NmdpCode,
                Serology = fourFieldAllele.Serology
            };
            var tgsAllele = FromThreeFieldAllele(threeFieldAllele, otherAllelesInSubtypeString);
            tgsAllele.FourFieldAllele = fourFieldAllele.AlleleName;
            return tgsAllele;
        }

        private static TgsAllele FromThreeFieldAllele(AlleleTestData threeFieldAllele, List<AlleleTestData> otherAllelesInSubtypeString)
        {
            var twoFieldAllele = new AlleleTestData
            {
                AlleleName = AlleleSplitter.RemoveLastField(threeFieldAllele.AlleleName),
                PGroup = threeFieldAllele.PGroup,
                GGroup = threeFieldAllele.GGroup,
                NmdpCode = threeFieldAllele.NmdpCode,
                Serology = threeFieldAllele.Serology
            };
            var tgsAllele = FromTwoFieldAllele(twoFieldAllele, otherAllelesInSubtypeString);
            tgsAllele.ThreeFieldAllele = threeFieldAllele.AlleleName;
            return tgsAllele;
        }

        private static TgsAllele FromTwoFieldAllele(AlleleTestData twoFieldAllele, List<AlleleTestData> otherAllelesInSubtypeString)
        {
            return new TgsAllele
            {
                TwoFieldAllele = twoFieldAllele.AlleleName,
                NmdpCode = twoFieldAllele.NmdpCode,
                Serology = twoFieldAllele.Serology,
                PGroup = twoFieldAllele.PGroup,
                GGroup = twoFieldAllele.GGroup,
                XxCode = $"{AlleleSplitter.FirstField(twoFieldAllele.AlleleName)}:XX",
                AlleleStringOfSubtypes = GenerateAlleleStringOfSubtypes(twoFieldAllele, otherAllelesInSubtypeString),
            };
        }

        private static string GenerateAlleleStringOfNames(AlleleTestData alleleTestData, List<AlleleTestData> otherAllelesInAlleleString)
        {
            return otherAllelesInAlleleString.IsNullOrEmpty()
                ? null
                : $"{alleleTestData.AlleleName}{AlleleSeparator}{otherAllelesInAlleleString.Select(a => a.AlleleName.Replace(AlleleNamePrefix, "")).StringJoin(AlleleSeparator)}";
        }

        private static string GenerateAlleleStringOfSubtypes(AlleleTestData twoFieldAllele, List<AlleleTestData> otherAllelesInAlleleString)
        {
            if (otherAllelesInAlleleString.IsNullOrEmpty())
            {
                return null;
            }

            if (!otherAllelesInAlleleString.All(a => AlleleSplitter.FirstField(a.AlleleName) == AlleleSplitter.FirstField(twoFieldAllele.AlleleName)))
            {
                throw new InvalidTestDataException("Cannot create allele string of subtypes from alleles that do not share a first field");
            }

            var otherSubFields = otherAllelesInAlleleString.Select(x => AlleleSplitter.SecondField(x.AlleleName)).StringJoin(AlleleSeparator);
            return $"{twoFieldAllele.AlleleName}{AlleleSeparator}{otherSubFields}";
        }

        /// <summary>
        /// Returns the most accurate TGS typing stored for the allele, either two, three, or four field
        /// </summary>
        public string TgsTypedAllele => FourFieldAllele ?? ThreeFieldAllele ?? TwoFieldAllele;

        private string FourFieldAllele { get; set; }
        private string ThreeFieldAllele { get; set; }
        private string TwoFieldAllele { get; set; }
        private string NmdpCode { get; set; }
        private string XxCode { get; set; }
        private string Serology { get; set; }
        private string PGroup { get; set; }
        private string GGroup { get; set; }
        private string AlleleStringOfNames { get; set; }
        private string AlleleStringOfNamesWithSinglePGroup { get; set; }
        private string AlleleStringOfNamesWithMultiplePGroups { get; set; }
        private string AlleleStringOfSubtypes { get; set; }

        public string GetHlaForResolution(HlaTypingResolution typingResolution)
        {
            return typingResolution switch
            {
                // If the TGS allele is three field, the 'truncated three field' version cannot exist
                HlaTypingResolution.ThreeFieldTruncatedAllele => FourFieldAllele != null ? ThreeFieldAllele : null,

                // If the TGS allele is two field, the 'truncated two field' version cannot exist
                HlaTypingResolution.TwoFieldTruncatedAllele => ThreeFieldAllele != null ? TwoFieldAllele : null,

                // Only name guaranteed to be unambiguous
                HlaTypingResolution.Unambiguous => FourFieldAllele,

                HlaTypingResolution.Tgs => TgsTypedAllele,
                HlaTypingResolution.XxCode => XxCode,
                HlaTypingResolution.NmdpCode => NmdpCode,
                HlaTypingResolution.Serology => Serology,
                HlaTypingResolution.PGroup => PGroup,
                HlaTypingResolution.GGroup => GGroup,
                HlaTypingResolution.Untyped => null,
                HlaTypingResolution.AlleleStringOfNames => AlleleStringOfNames,
                HlaTypingResolution.AlleleStringOfSubtypes => AlleleStringOfSubtypes,
                HlaTypingResolution.AlleleStringOfNamesWithSinglePGroup => AlleleStringOfNamesWithSinglePGroup,
                HlaTypingResolution.AlleleStringOfNamesWithMultiplePGroups => AlleleStringOfNamesWithMultiplePGroups,
                HlaTypingResolution.Arbitrary => RandomTypingResolution(),
                _ => throw new ArgumentOutOfRangeException(nameof(typingResolution), typingResolution, null)
            };
        }

        private string RandomTypingResolution()
        {
            // TODO: ATLAS-969: Weight this such that NMDP codes / XX codes are less frequent, to reduce time spent running hla update
            var options = new List<string>
            {
                FourFieldAllele,
                ThreeFieldAllele,
                TwoFieldAllele,
                Serology,
                NmdpCode,
                XxCode,
                AlleleStringOfNames,
                AlleleStringOfSubtypes,
                PGroup,
                GGroup
            }.Where(x => x != null).ToList();

            return options.GetRandomElement();
        }
    }
}