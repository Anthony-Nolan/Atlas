using System.Collections.Generic;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Repositories
{
    public static class GenotypeRepository
    {
        public static readonly IEnumerable<Genotype> Genotypes = new List<Genotype>
        {
            new Genotype
            {
                Hla = new PhenotypeInfo<TgsAllele>
                {
                    A_1 = TgsAllele.FromFourFieldAllele("29:01:01:01", "TODO", "TODO"),
                    A_2 = TgsAllele.FromFourFieldAllele("29:02:01:01", "TODO", "TODO"),
                    B_1 = TgsAllele.FromFourFieldAllele("44:03:01:01", "TODO", "TODO"),
                    B_2 = TgsAllele.FromFourFieldAllele("07:05:01:01", "TODO", "TODO"),
                    DRB1_1 = TgsAllele.FromFourFieldAllele("15:01:01:01", "TODO", "TODO"),
                    DRB1_2 = TgsAllele.FromFourFieldAllele("13:01:01:01", "TODO", "TODO"),
                    C_1 = TgsAllele.FromFourFieldAllele("07:02:01:03", "TODO", "TODO"),
                    C_2 = TgsAllele.FromFourFieldAllele("03:04:01:01", "TODO", "TODO"),
                    DQB1_1 = TgsAllele.FromFourFieldAllele("02:02:01:01", "TODO", "TODO"),
                    DQB1_2 = TgsAllele.FromFourFieldAllele("06:02:01:01", "TODO", "TODO"),
                    DPB1_1 = TgsAllele.FromFourFieldAllele("04:02:01:02", "TODO", "TODO"),
                    DPB1_2 = TgsAllele.FromThreeFieldAllele("03:01:01", "TODO", "TODO"),
                }
            }
        };
        
        /// <summary>
        /// A Genotype for which all hla values do not match any others in the repository
        /// </summary>
        public static readonly Genotype NonMatchingGenotype = new Genotype
        {
            Hla = new PhenotypeInfo<TgsAllele>
            {
                A_1 = TgsAllele.FromFourFieldAllele("29:01:01:11", "TODO", "TODO"),
                A_2 = TgsAllele.FromFourFieldAllele("29:02:01:11", "TODO", "TODO"),
                B_1 = TgsAllele.FromFourFieldAllele("44:03:01:11", "TODO", "TODO"),
                B_2 = TgsAllele.FromFourFieldAllele("07:05:01:11", "TODO", "TODO"),
                DRB1_1 = TgsAllele.FromFourFieldAllele("15:01:01:11", "TODO", "TODO"),
                DRB1_2 = TgsAllele.FromFourFieldAllele("13:01:01:11", "TODO", "TODO"),
                C_1 = TgsAllele.FromFourFieldAllele("07:02:01:13", "TODO", "TODO"),
                C_2 = TgsAllele.FromFourFieldAllele("03:04:01:11", "TODO", "TODO"),
                DQB1_1 = TgsAllele.FromFourFieldAllele("02:02:01:11", "TODO", "TODO"),
                DQB1_2 = TgsAllele.FromFourFieldAllele("06:02:01:11", "TODO", "TODO"),
                DPB1_1 = TgsAllele.FromFourFieldAllele("04:02:01:12", "TODO", "TODO"),
                DPB1_2 = TgsAllele.FromThreeFieldAllele("03:01:11", "TODO", "TODO"),
            }
        };
    }
}