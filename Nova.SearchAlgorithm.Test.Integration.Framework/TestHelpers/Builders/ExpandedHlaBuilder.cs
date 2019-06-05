using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.Common.Models;

namespace Nova.SearchAlgorithm.Test.Integration.TestHelpers.Builders
{
    public class ExpandedHlaBuilder
    {
        private readonly ExpandedHla hla;

        public ExpandedHlaBuilder()
        {
            hla = new ExpandedHla
            {
                Locus = Locus.A,
                LookupName = "HLA",
                OriginalName = "HLA",
                PGroups = new List<string>()
            };
        }

        public ExpandedHlaBuilder WithPGroups(params string[] pGroups)
        {
            hla.PGroups = hla.PGroups.Concat(pGroups);
            return this;
        }
        
        public ExpandedHla Build()
        {
            return hla;
        }
    }
}