using System;
using System.Linq;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Resources;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Models
{
    /// <summary>
    /// Test data will be generated from strongly types TGS data - i.e. in a 2, 3, or 4 field allele name format
    /// It will be 'dumbed down' to lower resolution typings. This class includes all lower resolution typiings for a given 3 or 4 field allele. 
    /// </summary>
    public class TgsAllele
    {
        public static TgsAllele FromFourFieldAllele(AlleleTestData fourFieldAllele, Locus locus)
        {
            var threeFieldAllele = new AlleleTestData
            {
                AlleleName = RemoveLastField(fourFieldAllele.AlleleName),
                PGroup = fourFieldAllele.PGroup,
                GGroup = fourFieldAllele.GGroup,
                NmdpCode = fourFieldAllele.NmdpCode,
                Serology = fourFieldAllele.Serology
            };
            var tgsAllele = FromThreeFieldAllele(threeFieldAllele, locus);
            tgsAllele.FourFieldAllele = fourFieldAllele.AlleleName;
            return tgsAllele;
        }

        public static TgsAllele FromThreeFieldAllele(AlleleTestData threeFieldAllele, Locus locus)
        {
            var twoFieldAllele = new AlleleTestData
            {
                AlleleName = RemoveLastField(threeFieldAllele.AlleleName),
                PGroup = threeFieldAllele.PGroup,
                GGroup = threeFieldAllele.GGroup,
                NmdpCode = threeFieldAllele.NmdpCode,
                Serology = threeFieldAllele.Serology
            };
            var tgsAllele = FromTwoFieldAllele(twoFieldAllele, locus);
            tgsAllele.ThreeFieldAllele = threeFieldAllele.AlleleName;
            return tgsAllele;
        }

        public static TgsAllele FromTwoFieldAllele(AlleleTestData twoFieldAllele, Locus locus)
        {
            return new TgsAllele
            {
                TwoFieldAllele = twoFieldAllele.AlleleName,
                NmdpCode = twoFieldAllele.NmdpCode,
                Serology = twoFieldAllele.Serology,
                XxCode = $"{FirstField(twoFieldAllele.AlleleName)}:XX",
                Locus = locus,
            };
        }

        private Locus Locus { get; set; }

        /// <summary>
        /// Returns the most accurate TGS typing stored for the allele, either three or four field
        /// </summary>
        public string TgsTypedAllele => FourFieldAllele ?? ThreeFieldAllele;

        private string FourFieldAllele { get; set; }
        public string ThreeFieldAllele { get; private set; }
        public string TwoFieldAllele { get; private set; }

        public string NmdpCode { get; private set; }
        public string XxCode { get; private set; }

        public string Serology { get; private set; }

        public string GetHlaForCategory(HlaTypingCategory typingCategory)
        {
            switch (typingCategory)
            {
                case HlaTypingCategory.TgsFourFieldAllele:
                    return TgsTypedAllele;
                case HlaTypingCategory.ThreeFieldTruncatedAllele:
                    return ThreeFieldAllele;
                case HlaTypingCategory.TwoFieldTruncatedAllele:
                    return TwoFieldAllele;
                case HlaTypingCategory.XxCode:
                    return XxCode;
                case HlaTypingCategory.NmdpCode:
                    return NmdpCode;
                case HlaTypingCategory.Serology:
                    return Serology;
                case HlaTypingCategory.Untyped:
                    return null;
                case HlaTypingCategory.TgsThreeFieldAllele:
                case HlaTypingCategory.TgsTwoFieldAllele:
                    throw new NotImplementedException();
                default:
                    throw new ArgumentOutOfRangeException(nameof(typingCategory), typingCategory, null);
            }
        }

        private static string RemoveLastField(string allele)
        {
            // TODO: NOVA-1571: Handle alleles with an expression suffix. This truncation will remove expression suffix.
            var splitAllele = allele.Split(':');
            return string.Join(":", splitAllele.Take(splitAllele.Length - 1));
        }

        private static string FirstField(string allele)
        {
            var splitAllele = allele.Split(':');
            return splitAllele.First();
        }
    }
}