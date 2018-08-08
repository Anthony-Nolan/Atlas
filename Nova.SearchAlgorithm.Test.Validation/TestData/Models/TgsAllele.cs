using System;
using System.Linq;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Models
{
    /// <summary>
    /// Test data will be generated from strongly types TGS data - i.e. in a 3 or 4 field allele name format
    /// It will be 'dumbed down' to lower resolution typings. This class includes all lower resolution typiings for a given 3 or 4 field allele. 
    /// </summary>
    public class TgsAllele
    {
        public static TgsAllele FromFourFieldAllele(string fourFieldAllele, string nmdpCode, string serology)
        {
            var threeFieldAllele = RemoveLastField(fourFieldAllele);
            var tgsAllele = FromThreeFieldAllele(threeFieldAllele, nmdpCode, serology);
            tgsAllele.FourFieldAllele = fourFieldAllele;
            return tgsAllele;
        }

        public static TgsAllele FromThreeFieldAllele(string threeFieldAllele, string nmdpCode, string serology)
        {
            return new TgsAllele
            {
                ThreeFieldAllele = threeFieldAllele,
                TwoFieldAllele = RemoveLastField(threeFieldAllele),
                NmdpCode = nmdpCode,
                Serology = serology,
                XxCode = $"{FirstField(threeFieldAllele)}:XX",
            };
        }

        private static string RemoveLastField(string allele)
        {
            var splitAllele = allele.Split(':');
            return string.Join(":", splitAllele.Take(splitAllele.Length - 1));
        }
        
        private static string FirstField(string allele)
        {
            var splitAllele = allele.Split(':');
            return splitAllele.First();
        }
        
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
                case HlaTypingCategory.Tgs:
                    return TgsTypedAllele;
                case HlaTypingCategory.ThreeField:
                    return ThreeFieldAllele;
                case HlaTypingCategory.TwoField:
                    return TwoFieldAllele;
                case HlaTypingCategory.XxCode:
                    return XxCode;
                case HlaTypingCategory.NmdpCode:
                    return NmdpCode;
                case HlaTypingCategory.Serology:
                    return Serology;
                default:
                    throw new ArgumentOutOfRangeException(nameof(typingCategory), typingCategory, null);
            }
        }
    }
}