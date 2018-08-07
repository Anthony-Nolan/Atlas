using System;
using System.Linq;

namespace Nova.SearchAlgorithm.Test.Validation.TestData
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
        
        public string FourFieldAllele { get; set; }
        public string ThreeFieldAllele { get; set; }
        public string TwoFieldAllele { get; set; }
        
        public string NmdpCode { get; set; }
        public string XxCode { get; set; }
        
        public string Serology { get; set; }
    }
}