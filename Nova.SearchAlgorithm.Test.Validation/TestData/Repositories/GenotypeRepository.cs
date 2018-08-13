using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Resources;
using static System.Linq.Enumerable;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Repositories
{
    public static class GenotypeRepository
    {
        static readonly Random random = new Random();
        
        private static PhenotypeInfo<List<TgsAllele>> TgsAlleles =>
            FourFieldAlleles.Alleles.Map((l, p, alleles) => alleles.Select(a => TgsAllele.FromFourFieldAllele(a, l)).ToList());

        public static readonly IEnumerable<Genotype> Genotypes = new List<Genotype>();

        /// <summary>
        /// Creates a random full Genotype from the available TGS allele names
        /// </summary>
        /// <returns></returns>
        public static Genotype NextGenotype()
        {
            return new Genotype
            {
                Hla = TgsAlleles.Map((locus, position, alleleNames) => alleleNames[random.Next(alleleNames.Count)])
            };
        }

        /// <summary>
        /// A Genotype for which all hla values do not match any others in the repository
        /// </summary>
        ///  TODO: NOVA-1590: Create more robust method of guaranteeing a mismatch
        /// As we're randomly selecting alleles for donors, there's a chance this will actually match
        public static readonly Genotype NonMatchingGenotype = new Genotype
        {
            Hla = NonMatchingAlleles.Alleles.Map((l, p, a) => TgsAllele.FromFourFieldAllele(a, l))
        };
    }
}