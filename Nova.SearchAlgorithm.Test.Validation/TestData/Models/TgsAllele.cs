using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Castle.Core.Internal;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Resources;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Models
{
    /// <summary>
    /// Test data will be generated from strongly types TGS data - i.e. in a 3 or 4 field allele name format
    /// It will be 'dumbed down' to lower resolution typings. This class includes all lower resolution typiings for a given 3 or 4 field allele. 
    /// </summary>
    public class TgsAllele
    {
        static readonly Random random = new Random();
        private static IEnumerable<string> relDnaSerFileContents;

        public static TgsAllele FromFourFieldAllele(string fourFieldAllele, Locus locus)
        {
            var threeFieldAllele = RemoveLastField(fourFieldAllele);
            var tgsAllele = FromThreeFieldAllele(threeFieldAllele, locus);
            tgsAllele.FourFieldAllele = fourFieldAllele;
            return tgsAllele;
        }

        public static TgsAllele FromThreeFieldAllele(string threeFieldAllele, Locus locus)
        {
            return new TgsAllele
            {
                ThreeFieldAllele = threeFieldAllele,
                TwoFieldAllele = RemoveLastField(threeFieldAllele),
                NmdpCode = GetNmdpCode(threeFieldAllele),
                Serology = GetSerology(threeFieldAllele, locus),
                XxCode = $"{FirstField(threeFieldAllele)}:XX",
                Locus = locus,
            };
        }

        public Locus Locus { get; set; }

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
        /// Selects a random nmdp string that corresponds to the first two fields of the allele string, as stored in the NmdpCodes class
        /// </summary>
        private static string GetNmdpCode(string threeFieldAlleleString)
        {
            var firstField = FirstField(threeFieldAlleleString);
            var firstTwoFields = (firstField + threeFieldAlleleString.Split(':')[1]).Replace("*", "");

            var possibleNmdpCodes = NmdpCodes.NmdpCodeLookup.Where(nmdp => nmdp.Key == firstTwoFields).ToList();
            var alphaNmdpCode = possibleNmdpCodes[random.Next(possibleNmdpCodes.Count)].Value;

            return $"{firstField}:{alphaNmdpCode}";
        }

        /// <summary>
        /// Selects a random serology typing that corresponds to the first two fields of the allele string, as stored in the NmdpCodes class
        /// </summary>
        private static string GetSerology(string threeFieldAllele, Locus locus)
        {
            if (relDnaSerFileContents == null)
            {
                var filePath = $"{TestContext.CurrentContext.TestDirectory}\\TestData\\Resources\\rel_dna_ser.txt";
                relDnaSerFileContents = File.ReadAllLines(filePath);
            }

            var line = relDnaSerFileContents.FirstOrDefault(l => l.Contains(threeFieldAllele.Replace("*", ""))
                                                                 && l.Contains(locus.ToString().ToUpper()));

            var serology =  line == null ? null : GetSerologyFromLine(line);
            return serology == "?" ? null : serology;
        }

        private static string GetSerologyFromLine(string line)
        {
            return line.Split(';').Skip(2).FirstOrDefault(s => !s.IsNullOrEmpty());
        }
    }
}