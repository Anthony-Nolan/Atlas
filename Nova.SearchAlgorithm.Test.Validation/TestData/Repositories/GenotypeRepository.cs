using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Resources;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Repositories
{
    public static class GenotypeRepository
    {
        static readonly Random random = new Random();
        
        public static PhenotypeInfo<List<string>> FourFieldHlas { get; set; }

        private static PhenotypeInfo<List<TgsAllele>> TgsAlleles =>
            FourFieldHlas.Map((l, p, alleles) => alleles.Select(a => TgsAllele.FromFourFieldAllele(a, l)).ToList());

        public static readonly IEnumerable<Genotype> Genotypes = new List<Genotype>();

        /// <summary>
        /// Creates a random full Genotype from the available TGS allele names
        /// </summary>
        /// <returns></returns>
        public static Genotype NextGenotype()
        {
            if (FourFieldHlas == null)
            {
                LoadFourFieldHlaFromFile();
            }

            return new Genotype
            {
                Hla = TgsAlleles.Map((locus, position, alleleNames) => alleleNames[random.Next(alleleNames.Count)])
            };
        }

        private static void LoadFourFieldHlaFromFile()
        {
            var assem = System.Reflection.Assembly.GetExecutingAssembly();
            using (var stream = assem.GetManifestResourceStream("Nova.SearchAlgorithm.Test.Validation.TestData.Resources.four_field_hla_names.json"))
            {
                using (var reader = new StreamReader(stream))
                {
                    FourFieldHlas = JsonConvert.DeserializeObject<PhenotypeInfo<List<string>>>(reader.ReadToEnd());
                }
            }
        }

        /// <summary>
        /// A Genotype for which all hla values do not match any others in the repository
        /// </summary>
        ///  TODO: NOVA-1590: Create more robust method of guaranteeing a mismatch
        /// As we're randomly selecting alleles for donors, there's a chance this will actually match
        public static readonly Genotype NonMatchingGenotype = new Genotype
        {
            Hla = new PhenotypeInfo<TgsAllele>
            {
                A_1 = TgsAllele.FromFourFieldAllele("29:01:01:11", Locus.A),
                A_2 = TgsAllele.FromFourFieldAllele("29:02:01:11", Locus.A),
                B_1 = TgsAllele.FromFourFieldAllele("44:03:01:11", Locus.B),
                B_2 = TgsAllele.FromFourFieldAllele("07:05:01:11", Locus.B),
                DRB1_1 = TgsAllele.FromFourFieldAllele("15:01:01:11", Locus.Drb1),
                DRB1_2 = TgsAllele.FromFourFieldAllele("13:01:01:11", Locus.Drb1),
                C_1 = TgsAllele.FromFourFieldAllele("07:02:01:13", Locus.C),
                C_2 = TgsAllele.FromFourFieldAllele("03:04:01:11", Locus.C),
                DQB1_1 = TgsAllele.FromFourFieldAllele("02:02:01:11", Locus.Dqb1),
                DQB1_2 = TgsAllele.FromFourFieldAllele("06:02:01:11", Locus.Dqb1),
                DPB1_1 = TgsAllele.FromFourFieldAllele("04:02:01:12", Locus.Dpb1),
                DPB1_2 = TgsAllele.FromThreeFieldAllele("85:01:01", Locus.Dpb1),
            }
        };
    }
}