using Castle.Core.Internal;
using Nova.SearchAlgorithm.Test.Validation.TestData.Exceptions;
using Nova.SearchAlgorithm.Test.Validation.TestData.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Models.Hla
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
        /// <param name="alleleStringOtherAlleles">Alleles to use in the various generated allele strings</param>
        public static TgsAllele FromTestDataAllele(
            AlleleTestData allele, 
            AlleleStringOtherAlleles alleleStringOtherAlleles)
        {
            TgsAllele tgsAllele;

            var fieldCount = AlleleSplitter.NumberOfFields(allele.AlleleName);
            switch (fieldCount)
            {
                case 4:
                    tgsAllele = FromFourFieldAllele(allele, alleleStringOtherAlleles.SubtypeString);
                    break;
                case 3:
                    tgsAllele = FromThreeFieldAllele(allele, alleleStringOtherAlleles.SubtypeString);
                    break;
                case 2:
                    tgsAllele = FromTwoFieldAllele(allele, alleleStringOtherAlleles.SubtypeString);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("TGS test allele of unexpected field count found: " + allele.AlleleName);
            }

            tgsAllele.AlleleStringOfNamesWithMultiplePGroups = GenerateAlleleStringOfNames(allele, alleleStringOtherAlleles.NameStringWithMultiplePGroups);
            tgsAllele.AlleleStringOfNamesWithSinglePGroup = GenerateAlleleStringOfNames(allele, alleleStringOtherAlleles.NameStringWithSinglePGroup);
            tgsAllele.AlleleStringOfNames = GenerateAlleleStringOfNames(allele, alleleStringOtherAlleles.NameString);
            return tgsAllele;
        }

        /// <summary>
        /// Create TGS Allele from test data source without setting the allele string properties.
        /// </summary>
        public static TgsAllele FromTestDataAllele(AlleleTestData allele)
        {
            return FromTestDataAllele(allele, new AlleleStringOtherAlleles());
        }

        private static TgsAllele FromFourFieldAllele(
            AlleleTestData fourFieldAllele,
            IEnumerable<AlleleTestData> otherAllelesInSubtypeString
        )
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

        private static TgsAllele FromThreeFieldAllele(
            AlleleTestData threeFieldAllele,
            IEnumerable<AlleleTestData> otherAllelesInSubtypeString
        )
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

        private static TgsAllele FromTwoFieldAllele(
            AlleleTestData twoFieldAllele,
            IEnumerable<AlleleTestData> otherAllelesInSubtypeString
        )
        {
            return new TgsAllele
            {
                TwoFieldAllele = twoFieldAllele.AlleleName,
                NmdpCode = twoFieldAllele.NmdpCode,
                Serology = twoFieldAllele.Serology,
                XxCode = $"{AlleleSplitter.FirstField(twoFieldAllele.AlleleName)}:XX",
                AlleleStringOfSubtypes = GenerateAlleleStringOfSubtypes(twoFieldAllele, otherAllelesInSubtypeString),
            };
        }

        private static string GenerateAlleleStringOfNames(AlleleTestData alleleTestData, IEnumerable<AlleleTestData> otherAllelesInAlleleString)
        {
            return otherAllelesInAlleleString.IsNullOrEmpty()
                ? null
                : $"{alleleTestData.AlleleName}{AlleleSeparator}{string.Join(AlleleSeparator, otherAllelesInAlleleString.Select(a => a.AlleleName.Replace(AlleleNamePrefix, "")))}";
        }

        private static string GenerateAlleleStringOfSubtypes(AlleleTestData twoFieldAllele, IEnumerable<AlleleTestData> otherAllelesInAlleleString)
        {
            if (otherAllelesInAlleleString.IsNullOrEmpty())
            {
                return null;
            }
            
            if (!otherAllelesInAlleleString.All(a => AlleleSplitter.FirstField(a.AlleleName) == AlleleSplitter.FirstField(twoFieldAllele.AlleleName)))
            {
                throw new InvalidTestDataException("Cannot create allele string of subtypes from alleles that do not share a first field");
            }

            var otherSubFields = string.Join(AlleleSeparator, otherAllelesInAlleleString.Select(x => AlleleSplitter.SecondField(x.AlleleName)));
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

        private string AlleleStringOfNames { get; set; }
        private string AlleleStringOfNamesWithSinglePGroup { get; set; }
        private string AlleleStringOfNamesWithMultiplePGroups { get; set; }
        private string AlleleStringOfSubtypes { get; set; }

        public string GetHlaForResolution(HlaTypingResolution typingResolution)
        {
            switch (typingResolution)
            {
                case HlaTypingResolution.Tgs:
                    return TgsTypedAllele;
                case HlaTypingResolution.ThreeFieldTruncatedAllele:
                    // If the TGS allele is three field, the 'truncated three field' version cannot exist
                    return FourFieldAllele != null ? ThreeFieldAllele : null;
                case HlaTypingResolution.TwoFieldTruncatedAllele:
                    // If the TGS allele is two field, the 'truncated two field' version cannot exist
                    return ThreeFieldAllele != null ? TwoFieldAllele : null;
                case HlaTypingResolution.XxCode:
                    return XxCode;
                case HlaTypingResolution.NmdpCode:
                    return NmdpCode;
                case HlaTypingResolution.Serology:
                    return Serology;
                case HlaTypingResolution.Untyped:
                    return null;
                case HlaTypingResolution.Arbitrary:
                    // TODO: NOVA-1665: Weight this such that NMDP codes / XX codes are less frequent, to reduce time spent running hla update
                    var options = new List<string>
                    {
                        FourFieldAllele,
                        ThreeFieldAllele,
                        TwoFieldAllele,
                        Serology,
                        NmdpCode,
                        XxCode
                    }.Where(x => x != null).ToList();

                    return options.GetRandomElement();
                case HlaTypingResolution.AlleleStringOfNames:
                    return AlleleStringOfNames;
                case HlaTypingResolution.AlleleStringOfSubtypes:
                    return AlleleStringOfSubtypes;
                case HlaTypingResolution.Unambiguous:
                    // only name guaranteed to be unambiguous
                    return FourFieldAllele;
                case HlaTypingResolution.AlleleStringOfNamesWithSinglePGroup:
                    return AlleleStringOfNamesWithSinglePGroup;
                case HlaTypingResolution.AlleleStringOfNamesWithMultiplePGroups:
                    return AlleleStringOfNamesWithMultiplePGroups;
                default:
                    throw new ArgumentOutOfRangeException(nameof(typingResolution), typingResolution, null);
            }
        }
    }
}