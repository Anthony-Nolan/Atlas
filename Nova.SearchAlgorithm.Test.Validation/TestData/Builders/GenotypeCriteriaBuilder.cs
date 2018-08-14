using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models.Hla;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Builders
{
    public class GenotypeCriteriaBuilder
    {
        private readonly GenotypeCriteria genotypeCriteria;

        public GenotypeCriteriaBuilder()
        {
            genotypeCriteria = new GenotypeCriteria
            {
                HasNonUniquePGroups = new PhenotypeInfo<bool>()
            };
        }

        public GenotypeCriteriaBuilder WithNonUniquePGroupsAtAllLoci()
        {
            genotypeCriteria.HasNonUniquePGroups = new PhenotypeInfo<bool>
            {
                A_1 = true,
                A_2 = true,
                B_1 = true,
                B_2 = true,
                C_1 = true,
                C_2 = true,
                DPB1_1 = true,
                DPB1_2 = true,
                DQB1_1 = true,
                DQB1_2 = true,
                DRB1_1 = true,
                DRB1_2 = true
            };
            
            return this;
        }
        
        public GenotypeCriteria Build()
        {
            return genotypeCriteria;
        }
    }
}