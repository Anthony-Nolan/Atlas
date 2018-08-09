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
    /// Test data will be generated from strongly types TGS data - i.e. in a 2, 3, or 4 field allele name format
    /// It will be 'dumbed down' to lower resolution typings. This class includes all lower resolution typiings for a given 3 or 4 field allele. 
    /// </summary>
    public class TgsAllele
    {
        private static IEnumerable<string> relDnaSerFileContents;

        public static TgsAllele FromFourFieldAllele(string fourFieldAllele, Locus locus)
        {
            var threeFieldAllele = RemoveLastField(fourFieldAllele);
            var tgsAllele = FromThreeFieldAllele(threeFieldAllele, locus, GetSerology(fourFieldAllele, locus));
            tgsAllele.FourFieldAllele = fourFieldAllele;
            return tgsAllele;
        }

        public static TgsAllele FromThreeFieldAllele(string threeFieldAllele, Locus locus, string serology = null)
        {
            var twoFieldAllele = RemoveLastField(threeFieldAllele);
            var tgsAllele = FromTwoFieldAllele(twoFieldAllele, locus, serology ?? GetSerology(threeFieldAllele, locus));
            tgsAllele.ThreeFieldAllele = threeFieldAllele;
            return tgsAllele;
        }

        public static TgsAllele FromTwoFieldAllele(string twoFieldAllele, Locus locus, string serology = null)
        {
            return new TgsAllele
            {
                TwoFieldAllele = twoFieldAllele,
                NmdpCode = GetNmdpCode(twoFieldAllele, locus),
                Serology = serology ?? GetSerology(twoFieldAllele, locus),
                XxCode = $"{FirstField(twoFieldAllele)}:XX",
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
                default:
                    throw new ArgumentOutOfRangeException(nameof(typingCategory), typingCategory, null);
            }
        }

        private static string RemoveLastField(string allele)
        {
            // TODO: NOVA-1571: Handle expressing alleles. This truncation will remove expression suffix.
            var splitAllele = allele.Split(':');
            return string.Join(":", splitAllele.Take(splitAllele.Length - 1));
        }

        private static string FirstField(string allele)
        {
            var splitAllele = allele.Split(':');
            return splitAllele.First();
        }

        /// <summary>
        /// Selects an nmdp string that corresponds to a two field allele string, as stored in the NmdpCodes class
        /// These NMDP Codes are manually curated
        /// </summary>
        private static string GetNmdpCode(string twoFieldAlleleString, Locus locus)
        {
            var firstField = FirstField(twoFieldAlleleString);

            var locusNmdpCodes = NmdpCodes.NmdpCodeLookup.DataAtLocus(locus);
            var nmdpCode = locusNmdpCodes.Item1.Concat(locusNmdpCodes.Item2)
                .First(nmdp => nmdp.Key == twoFieldAlleleString.Replace("*", "")).Value;

            return $"{firstField}:{nmdpCode}";
        }

        /// <summary>
        /// Looks up the corresponding serology from a local version of the rel_dna_ser.txt file from WMDA
        /// </summary>
        private static string GetSerology(string allele, Locus locus)
        {
            if (relDnaSerFileContents == null)
            {
                var filePath = $"{TestContext.CurrentContext.TestDirectory}\\TestData\\Resources\\rel_dna_ser.txt";
                relDnaSerFileContents = File.ReadAllLines(filePath);
            }

            var line = relDnaSerFileContents.FirstOrDefault(l => l.Contains($";{allele.Replace("*", "")};")
                                                                 && l.Contains(locus.ToString().ToUpper()));

            var serology = line == null ? null : GetSerologyFromLine(line);
            return serology == "?" ? null : serology;
        }

        private static string GetSerologyFromLine(string line)
        {
            return line
                .Split(';')
                // First two values are locus and hla name
                .Skip(2)
                .First(s => !s.IsNullOrEmpty())
                ?.Split('/')
                .First(s => s != "0");
        }
    }
}